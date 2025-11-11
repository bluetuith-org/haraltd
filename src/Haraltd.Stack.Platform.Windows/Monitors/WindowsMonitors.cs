using System.Collections.Frozen;
using System.Timers;
using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.OutputStream;
using Haraltd.Stack.Base.Monitors;
using Haraltd.Stack.Platform.Windows.Adapters;
using Haraltd.Stack.Platform.Windows.Devices;
using Haraltd.Stack.Platform.Windows.PlatformSpecific;
using Nefarius.Utilities.Bluetooth;
using Nefarius.Utilities.DeviceManagement.PnP;
using Windows.Devices.Bluetooth;
using Windows.Devices.Radios;
using Windows.Win32;
using WmiLight;
using Lock = DotNext.Threading.Lock;
using Timer = System.Timers.Timer;

namespace Haraltd.Stack.Platform.Windows.Monitors;

internal static partial class WindowsMonitors
{
    private static bool IsRunning { get; set; }
    private static StateMonitor _stateMonitor;

    private static readonly FrozenSet<MonitorObserver> Monitors =
    [
        new AdapterStateMonitors(),
        new DevicesStateMonitors(),
        new OppServerMonitor(),
        DiscoveryWatcher.Instance,
    ];

    internal static ErrorData Start(OperationToken token)
    {
        if (IsRunning)
            return Errors.ErrorNone;

        try
        {
            var stateMonitor = new StateMonitor(token);
            stateMonitor.Initialize();

            foreach (var monitor in Monitors)
                monitor.Initialize(token);

            _stateMonitor = stateMonitor;
            IsRunning = true;
        }
        catch (Exception e)
        {
            return Errors.ErrorUnexpected.AddMetadata("exception", e.Message);
        }

        return Errors.ErrorNone;
    }

    internal static ErrorData Stop()
    {
        if (!IsRunning)
            return Errors.ErrorNone;

        _stateMonitor?.Dispose();
        IsRunning = false;

        return Errors.ErrorNone;
    }

    internal static void PushDiscoverableEvent() => _stateMonitor?.DiscoverableEvent();

    private static void PublishAdapterEvent(AdapterEvent adapterEvent)
    {
        foreach (var monitor in Monitors)
            monitor.AdapterEventHandler(adapterEvent);
    }

    private static void PublishDeviceEvent(DeviceEvent deviceEvent)
    {
        foreach (var monitor in Monitors)
            monitor.DeviceEventHandler(deviceEvent);
    }

    private partial class StateMonitor(OperationToken token) : IDisposable
    {
        private BluetoothAdapter _adapter;
        private Radio _radio;

        private readonly Timer _timer = new(TimeSpan.FromSeconds(1)) { AutoReset = true };

        private DevicesWatcher _monitor;
        private RegistryWatcher _batteryStatusWatcher;
        private DevicesWatcher.Subscriber _subscriber;

        private DeviceNotificationListener _listener;

        private static AdapterEvent _adapterAttachedEvent = new();
        private static AdapterEvent _adapterPoweredEvent = new();
        private static AdapterEvent _adapterDiscoverableEvent = new();

        private readonly Lock _adapterLock = Lock.Semaphore(new SemaphoreSlim(1, 1));
        private readonly Lock _deviceLock = Lock.Semaphore(new SemaphoreSlim(1, 1));

        internal void Initialize()
        {
            _listener = new DeviceNotificationListener();

            _listener.RegisterDeviceArrived(
                AdapterAttachedEventHandler,
                WindowsPnPInformation.BluetoothRadiosInterfaceGuid
            );
            _listener.RegisterDeviceRemoved(
                AdapterDetachedEventHandler,
                WindowsPnPInformation.BluetoothRadiosInterfaceGuid
            );

            _listener.StartListen(WindowsPnPInformation.BluetoothRadiosInterfaceGuid);
            StartAdapterMonitor();

            _timer.Elapsed += AdapterDiscoverableStateChanged;

            _batteryStatusWatcher = new RegistryWatcher(
                "HKEY_LOCAL_MACHINE",
                WindowsPnPInformation.HfEnumeratorRegistryKey,
                DeviceBatteryStatusChanged
            );
            _batteryStatusWatcher.Start();
        }

        private void StartAdapterMonitor()
        {
            if (!_adapterLock.TryAcquire(out var holder))
                return;

            using var _ = holder;

            Retry:
            try
            {
                var adapter = BluetoothAdapter.GetDefaultAsync().GetAwaiter().GetResult();
                if (adapter == null)
                    return;

                var radio = adapter.GetRadioAsync().GetAwaiter().GetResult();
                if (radio == null)
                    return;

                _adapter = adapter;
                _radio = radio;

                _radio.StateChanged += RadioOnStateChanged;

                var adapterEvent = new AdapterEvent();
                adapterEvent.MergeRadioData();

                _adapterAttachedEvent = adapterEvent;

                _adapterPoweredEvent = WindowsAdapterExtensions.GetEmptyAdapterEventData(out var _);
                _adapterPoweredEvent.Action = IEvent.EventAction.Updated;

                _adapterDiscoverableEvent = _adapterPoweredEvent with
                {
                    OptionDiscoverable = PInvoke.BluetoothIsDiscoverable(null) > 0,
                };

                _timer.Start();

                StartDeviceMonitor();
            }
            catch
            {
                _adapterLock.TryAcquire(TimeSpan.FromSeconds(1), out var __);
                goto Retry;
            }
        }

        private void StopAdapterMonitor()
        {
            if (!_adapterLock.TryAcquire(out var holder))
                return;

            using var _ = holder;

            try
            {
                if (_radio == null)
                    return;

                _radio.StateChanged -= RadioOnStateChanged;
                _radio = null;
                _adapter = null;

                _adapterAttachedEvent.UuiDs = null;
                _adapterAttachedEvent.OptionPowered = false;
                _adapterAttachedEvent.OptionDiscoverable = false;
                _adapterAttachedEvent.OptionDiscovering = false;

                _timer.Stop();
                StopDeviceMonitor();
            }
            catch
            {
                //ignored
            }
        }

        private void StartDeviceMonitor()
        {
            if (!_deviceLock.TryAcquire(out var holder))
                return;

            using var _ = holder;

            try
            {
                if (_monitor != null)
                    return;

                _monitor = new DevicesWatcher(
                    BluetoothDevice.GetDeviceSelectorFromPairingState(true)
                );

                _subscriber = new DevicesWatcher.Subscriber(_monitor, token)
                {
                    OnAdded = static (info, tok) =>
                    {
                        if (info.OptionPaired.HasValue && info.OptionPaired.Value)
                        {
                            info.RefreshInformation();
                            PublishDeviceEvent(info);
                        }
                    },
                    OnRemoved = static (info, tok) =>
                    {
                        PublishDeviceEvent(info);
                    },
                    OnUpdated = static (_, newInfo, tok) =>
                    {
                        var isPaired = newInfo.OptionPaired.HasValue && newInfo.OptionPaired.Value;

                        if (isPaired)
                            PublishDeviceEvent(newInfo);
                    },
                };

                _monitor.Start();
            }
            catch
            {
                _monitor?.Dispose();
                _monitor = null;
            }
        }

        private void StopDeviceMonitor()
        {
            if (!_deviceLock.TryAcquire(out var holder))
                return;

            using var _ = holder;

            try
            {
                if (_monitor == null)
                    return;

                _subscriber?.Dispose();
                _subscriber = null;

                _monitor.Dispose();
                _monitor = null;
            }
            catch
            {
                // ignored
            }
        }

        internal void DiscoverableEvent() => AdapterDiscoverableStateChanged(null, null);

        private void AdapterAttachedEventHandler(DeviceEventArgs e)
        {
            StartAdapterMonitor();

            _adapterAttachedEvent.Action = IEvent.EventAction.Added;
            PublishAdapterEvent(_adapterAttachedEvent with { });
        }

        private void AdapterDetachedEventHandler(DeviceEventArgs _)
        {
            StopAdapterMonitor();

            _adapterAttachedEvent.Action = IEvent.EventAction.Removed;
            PublishAdapterEvent(_adapterAttachedEvent with { });
        }

        private void AdapterDiscoverableStateChanged(object sender, ElapsedEventArgs e)
        {
            if (_radio == null || _radio.State != RadioState.On)
                return;

            var discoverable = PInvoke.BluetoothIsDiscoverable(null) > 0;
            if (
                _adapterDiscoverableEvent.OptionDiscoverable.HasValue
                && _adapterDiscoverableEvent.OptionDiscoverable.Value == discoverable
            )
                return;

            _adapterDiscoverableEvent.OptionDiscoverable = discoverable;
            PublishAdapterEvent(_adapterDiscoverableEvent with { });
        }

        private async void DeviceBatteryStatusChanged(object s, WmiEventArrivedEventArgs e)
        {
            try
            {
                var roothPathEvent = e.NewEvent.GetPropertyValue("RootPath");
                if (roothPathEvent is string)
                {
                    var instances = 0;
                    while (
                        Devcon.FindByInterfaceGuid(
                            WindowsPnPInformation.HfEnumeratorInterfaceGuid,
                            out var pnpDevice,
                            instances++,
                            HostRadio.IsOperable
                        )
                    )
                        if (
                            Convert.ToInt32(
                                pnpDevice.GetProperty<byte>(
                                    WindowsPnPInformation.Device.BatteryPercentage
                                )
                            )
                            is var batteryPercentage
                                and > 0
                        )
                        {
                            var deviceEvent = new DeviceEvent
                            {
                                Action = IEvent.EventAction.Updated,
                            };

                            var error = await deviceEvent.MergeBatteryInformationAsync(
                                pnpDevice.DeviceId,
                                batteryPercentage,
                                token
                            );

                            if (error == Errors.ErrorNone)
                                Output.Event(deviceEvent, token);
                        }
                }
            }
            catch
            {
                // ignored
            }
        }

        private void RadioOnStateChanged(Radio sender, object args)
        {
            switch (sender.State)
            {
                case RadioState.On:
                    _adapterPoweredEvent.Action =
                        _adapterPoweredEvent.Action == IEvent.EventAction.Removed
                            ? IEvent.EventAction.Added
                            : IEvent.EventAction.Updated;

                    _adapterPoweredEvent.OptionPowered = true;
                    _adapterPoweredEvent.OptionPairable = true;

                    StartDeviceMonitor();
                    break;

                case RadioState.Off:
                    _adapterPoweredEvent.Action = IEvent.EventAction.Updated;

                    _adapterPoweredEvent.OptionPowered = false;
                    _adapterPoweredEvent.OptionPairable = false;
                    _adapterPoweredEvent.OptionDiscoverable = false;

                    StopDeviceMonitor();
                    break;

                case RadioState.Disabled:
                case RadioState.Unknown:
                    _adapterPoweredEvent.Action = IEvent.EventAction.Removed;
                    _adapterPoweredEvent.OptionPowered = false;

                    StopDeviceMonitor();
                    break;
            }

            PublishAdapterEvent(_adapterPoweredEvent with { });
        }

        public void Dispose()
        {
            try
            {
                _batteryStatusWatcher?.Dispose();

                _subscriber?.Dispose();
                _monitor?.Dispose();

                if (_radio != null)
                    _radio.StateChanged -= RadioOnStateChanged;

                _timer.Dispose();

                _listener?.StopListen();
                _listener?.UnregisterDeviceArrived(AdapterAttachedEventHandler);
                _listener?.UnregisterDeviceRemoved(AdapterDetachedEventHandler);
                _listener?.Dispose();
            }
            catch
            {
                // ignored
            }
        }
    }
}
