using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Timers;
using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.Managers;
using Haraltd.Operations.OutputStream;
using Haraltd.Stack.Base.Monitors;
using IOBluetooth;
using Lock = DotNext.Threading.Lock;
using Timer = System.Timers.Timer;

namespace Haraltd.Stack.Platform.MacOS.Monitors;

public static class DiscoveryMonitor
{
    private record DeviceInformation(DeviceEvent CachedDeviceEvent)
    {
        public readonly DeviceEvent CachedDeviceEvent = CachedDeviceEvent;
        public int DiscoveredCount;

        public string AddressKey => CachedDeviceEvent.Address;
    }

    private enum DiscoverySyncEventType : byte
    {
        DeviceDiscovered,
        RemovedDevicesTimerElapsed,
        PublishDiscoveryEvent,
        PublishDiscoveryEventAllDevices,
        RemoveDeviceFromCache,
    }

    private readonly record struct DiscoverySyncEvent(
        DiscoverySyncEventType EventType,
        DeviceEvent DeviceEvent,
        BluetoothDevice Device,
        IEvent.EventAction Action,
        OperationToken Token
    );

    internal static readonly DiscoveryStateMonitor MonitorInstance = new();

    private static int _scanCount;
    private static bool _started;

    private static Channel<DiscoverySyncEvent> _syncChannel = null!;

    private static readonly DiscoveryDelegate DeviceInquiryDelegate = new();
    private static readonly DeviceInquiry InquiryMonitor = new();
    private static readonly Dictionary<string, DeviceInformation> CurrentDevices = new(
        StringComparer.OrdinalIgnoreCase
    );

    private static readonly ConcurrentDictionary<Guid, OperationToken> Clients = new();

    private static readonly Timer RemovedDevicesCheckTimer;
    private static readonly Timer StopDiscoveryTimer;

    private static string AdapterAddress => MacOsMonitors.CurrentAdapterAddress;

    private static readonly Lock DiscoveryLock = Lock.Monitor();

    static DiscoveryMonitor()
    {
        InquiryMonitor.Delegate = DeviceInquiryDelegate;

        RemovedDevicesCheckTimer = new Timer(TimeSpan.FromSeconds(15));
        RemovedDevicesCheckTimer.Elapsed += RemovedDevicesCheckTimerOnElapsed;
        RemovedDevicesCheckTimer.AutoReset = true;

        StopDiscoveryTimer = new Timer(TimeSpan.FromSeconds(60));
        StopDiscoveryTimer.Elapsed += StopDiscoveryTimerOnElapsed;
        StopDiscoveryTimer.AutoReset = false;
    }

    private static void StopDiscoveryTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        Stop();
    }

    private static void RemovedDevicesCheckTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        ProcessDiscoveryEvent(DiscoverySyncEventType.RemovedDevicesTimerElapsed);
    }

    private static ErrorData Start()
    {
        using var _ = DiscoveryLock.Acquire();

        if (_started)
            return Errors.ErrorNone;

        StartSyncQueue();

        var ret = InquiryMonitor.Start();
        if (ret != 0)
        {
            StopSyncQueue();

            return Errors.ErrorDeviceDiscovery.AddMetadata(
                "exception",
                "Couldn't start discovery monitor"
            );
        }

        RemovedDevicesCheckTimer.Start();

        _started = true;

        return Errors.ErrorNone;
    }

    private static ErrorData Stop()
    {
        using var _ = DiscoveryLock.Acquire();

        if (!_started)
            return Errors.ErrorNone;

        _started = false;
        _scanCount = 0;

        StopSyncQueue();

        InquiryMonitor.Stop();
        InquiryMonitor.ClearFoundDevices();
        CurrentDevices.Clear();

        RemovedDevicesCheckTimer.Stop();

        return Errors.ErrorNone;
    }

    public static ErrorData StartForClient(OperationToken token, int timeout)
    {
        if (HostController.DefaultController.PowerState == HciPowerState.Off)
            return Errors.ErrorAdapterStateAccess.AddMetadata(
                "exception",
                "Adapter is not powered on"
            );

        StopDiscoveryTimer.Stop();

        var error = Start();
        if (error != Errors.ErrorNone)
            return error;

        error = MacOsMonitors.Start(token);
        if (error != Errors.ErrorNone)
            return error;

        if (Clients.TryGetValue(token.ClientId, out _))
            return Errors.ErrorDeviceDiscovery.AddMetadata(
                "exception",
                "Device discovery is already running"
            );

        var adapter = GetCurrentAdapter(true);

        Output.ClientEvent(adapter, token);
        PublishDiscoveryEventAllDevices(IEvent.EventAction.Added, token);

        OperationManager.SetOperationProperties(token, true, true);
        Clients.TryAdd(token.ClientId, token);

        if (timeout > 0)
            token.ReleaseAfter(timeout * 1000);

        Task.Run(() =>
        {
            token.Wait();
            StopForClient(token);
        });

        return Errors.ErrorNone;
    }

    public static ErrorData StopForClient(OperationToken token)
    {
        if (!Clients.TryRemove(token.ClientId, out var clientToken))
            return Errors.ErrorDeviceDiscovery.AddMetadata(
                "exception",
                "Device discovery is not running"
            );

        clientToken.Release();

        var adapter = GetCurrentAdapter(false);

        Output.ClientEvent(adapter, token);
        PublishDiscoveryEventAllDevices(IEvent.EventAction.Removed, token);

        if (Clients.IsEmpty)
            StopDiscoveryTimer.Start();

        return Errors.ErrorNone;
    }

    private static AdapterEvent GetCurrentAdapter(bool discoveringState)
    {
        return new AdapterEvent
        {
            Address = AdapterAddress,
            Action = IEvent.EventAction.Updated,
            OptionDiscovering = discoveringState,
        };
    }

    private static void PublishDiscoveryEvent(
        DeviceEvent deviceEvent,
        IEvent.EventAction action = IEvent.EventAction.None,
        OperationToken token = default
    )
    {
        ProcessDiscoveryEvent(
            DiscoverySyncEventType.PublishDiscoveryEvent,
            deviceEvent,
            null!,
            action == IEvent.EventAction.None ? deviceEvent.Action : action,
            token
        );
    }

    private static void PublishDiscoveryEventAllDevices(
        IEvent.EventAction action,
        OperationToken token = default
    )
    {
        ProcessDiscoveryEvent(
            DiscoverySyncEventType.PublishDiscoveryEventAllDevices,
            null!,
            null!,
            action,
            token
        );
    }

    private static void StartSyncQueue()
    {
        _syncChannel = Channel.CreateUnbounded<DiscoverySyncEvent>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = true,
            }
        );

        Task.Run(static async () =>
        {
            while (await _syncChannel.Reader.WaitToReadAsync())
            {
                while (_syncChannel.Reader.TryRead(out var discoveryEvent))
                {
                    HandleSyncEvent(ref discoveryEvent);
                }
            }
        });
    }

    private static void StopSyncQueue()
    {
        _syncChannel?.Writer.TryComplete();
        _syncChannel = null!;
    }

    private static void ProcessDiscoveryEvent(
        DiscoverySyncEventType eventType,
        DeviceEvent deviceEvent = null!,
        BluetoothDevice device = null!,
        IEvent.EventAction action = IEvent.EventAction.None,
        OperationToken token = default
    )
    {
        _syncChannel?.Writer.TryWrite(
            new DiscoverySyncEvent(eventType, deviceEvent, device, action, token)
        );
    }

    private static void HandleSyncEvent(ref DiscoverySyncEvent discoveryEvent)
    {
        try
        {
            switch (discoveryEvent.EventType)
            {
                case DiscoverySyncEventType.DeviceDiscovered:
                    var device = discoveryEvent.Device;
                    if (device == null)
                        return;

                    var address = discoveryEvent.Device.AddressToString;

                    if (CurrentDevices.TryGetValue(address, out var deviceInfo))
                    {
                        deviceInfo.CachedDeviceEvent.ResetProperties();
                        deviceInfo.CachedDeviceEvent.AppendDeviceEventData(
                            device,
                            IEvent.EventAction.Updated,
                            AdapterAddress,
                            default
                        );

                        deviceInfo.DiscoveredCount = _scanCount + 1;
                        PublishDiscoveryEvent(deviceInfo.CachedDeviceEvent);
                    }
                    else
                    {
                        var discoveredDeviceEvent = device.ToDeviceEvent(
                            IEvent.EventAction.None,
                            AdapterAddress
                        );

                        deviceInfo = new DeviceInformation(discoveredDeviceEvent)
                        {
                            DiscoveredCount = _scanCount + 1,
                        };

                        CurrentDevices.TryAdd(address, deviceInfo);
                        PublishDiscoveryEvent(discoveredDeviceEvent, IEvent.EventAction.Added);
                    }

                    break;

                case DiscoverySyncEventType.RemovedDevicesTimerElapsed:
                    InquiryMonitor.ClearFoundDevices();

                    _scanCount++;

                    var recentCount = -1;
                    var devices = CurrentDevices.OrderByDescending(pair =>
                        pair.Value.DiscoveredCount
                    );

                    foreach (var (_, deviceInformation) in devices)
                    {
                        var discoveredCount = deviceInformation.DiscoveredCount;
                        if (recentCount < 0)
                            recentCount = discoveredCount;

                        if (discoveredCount >= _scanCount && discoveredCount >= recentCount - 1)
                            continue;

                        CurrentDevices.Remove(deviceInformation.AddressKey, out var removedDevice);

                        if (removedDevice?.CachedDeviceEvent != null)
                            PublishDiscoveryEvent(
                                removedDevice.CachedDeviceEvent,
                                IEvent.EventAction.Removed
                            );
                    }

                    break;

                case DiscoverySyncEventType.PublishDiscoveryEvent:
                    var deviceEvent = discoveryEvent.DeviceEvent;
                    if (discoveryEvent.Action != IEvent.EventAction.None)
                        deviceEvent.Action = discoveryEvent.Action;

                    if (discoveryEvent.Token != default)
                    {
                        Output.ClientEvent(deviceEvent, discoveryEvent.Token);
                        return;
                    }

                    foreach (var clientToken in Clients.Values)
                    {
                        Output.ClientEvent(deviceEvent, clientToken);
                    }

                    break;

                case DiscoverySyncEventType.PublishDiscoveryEventAllDevices:
                    foreach (var deviceInformation in CurrentDevices)
                    {
                        PublishDiscoveryEvent(
                            deviceInformation.Value.CachedDeviceEvent,
                            discoveryEvent.Action,
                            discoveryEvent.Token
                        );
                    }

                    break;

                case DiscoverySyncEventType.RemoveDeviceFromCache:
                    CurrentDevices.Remove(discoveryEvent.DeviceEvent.Address, out _);
                    break;
            }
        }
        catch
        {
            // ignored
        }
    }

    internal class DiscoveryStateMonitor : MonitorObserver
    {
        public override void StopAndRelease()
        {
            _ = Stop();
        }

        public override void AdapterEventHandler(AdapterEvent adapterEvent)
        {
            if (adapterEvent.OptionPowered is false)
            {
                Task.Run(static () =>
                {
                    foreach (var token in Clients.Values)
                        StopForClient(token);

                    Stop();
                });
            }
        }

        public override void DeviceEventHandler(DeviceEvent deviceEvent)
        {
            if (
                deviceEvent.OptionPaired is true
                || deviceEvent.Action is IEvent.EventAction.Removed
            )
                ProcessDiscoveryEvent(DiscoverySyncEventType.RemoveDeviceFromCache, deviceEvent);
        }
    }

    private class DiscoveryDelegate : DeviceInquiryDelegate
    {
        public override void DeviceFound(DeviceInquiry sender, BluetoothDevice device)
        {
            if (device.IsPaired)
                return;

            ProcessDiscoveryEvent(DiscoverySyncEventType.DeviceDiscovered, null!, device);
        }
    }
}
