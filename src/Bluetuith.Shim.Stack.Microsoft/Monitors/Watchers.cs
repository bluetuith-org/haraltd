using Bluetuith.Shim.DataTypes;
using Bluetuith.Shim.Monitors;
using Bluetuith.Shim.Operations;
using Nefarius.Utilities.Bluetooth;
using Nefarius.Utilities.DeviceManagement.PnP;
using Windows.Devices.Bluetooth;
using static Bluetuith.Shim.DataTypes.IEvent;

namespace Bluetuith.Shim.Stack.Microsoft;

internal static class Watchers
{
    private static bool _isAdapterWatcherRunning = false;
    private static bool _isDeviceWatcherRunning = false;
    private static bool _isBatteryWatcherRunning = false;

    internal static bool IsDeviceWatcherRunning
    {
        get => _isDeviceWatcherRunning;
    }

    internal static async Task<ErrorData> Setup(
        OperationToken token,
        CancellationTokenSource waitForResume
    )
    {
        try
        {
            if (!waitForResume.IsCancellationRequested)
                waitForResume.Token.WaitHandle.WaitOne();

            await Task.WhenAll(
                new List<Task>()
                {
                    Task.Run(() => SetupAdapterWatcher(token)),
                    Task.Run(() => SetupDevicesWatcher(token)),
                    Task.Run(() => SetupBatteryWatcher(token)),
                }
            );
        }
        catch (AggregateException ae)
        {
            return Errors.ErrorUnexpected.WrapError(new() { { "exception", ae.Message } });
        }

        return Errors.ErrorNone;
    }

    private static void SetupAdapterWatcher(OperationToken token)
    {
        if (_isAdapterWatcherRunning)
            return;

        if (token.CancelTokenSource.IsCancellationRequested)
            return;

        try
        {
            var poweredStateMonitor = new AdapterPowerStateMonitor(token);

            poweredStateMonitor.Start();
            AdapterDiscoverableEventMonitor.Start(token);

            token.Wait();

            poweredStateMonitor.Stop();
            AdapterDiscoverableEventMonitor.Stop();
        }
        finally
        {
            _isAdapterWatcherRunning = false;
        }
    }

    private static void SetupBatteryWatcher(OperationToken token)
    {
        if (_isBatteryWatcherRunning)
            return;

        if (token.CancelTokenSource.IsCancellationRequested)
            return;

        try
        {
            using var watcher = new RegistryWatcher(
                "HKEY_LOCAL_MACHINE",
                WindowsPnPInformation.HFEnumeratorRegistryKey,
                (s, e) =>
                {
                    try
                    {
                        var roothPathEvent = e.NewEvent.GetPropertyValue("RootPath");
                        if (roothPathEvent != null && roothPathEvent is string rootPath)
                        {
                            var instances = 0;
                            while (
                                Devcon.FindByInterfaceGuid(
                                    WindowsPnPInformation.HFEnumeratorInterfaceGuid,
                                    out var pnpDevice,
                                    instances++,
                                    HostRadio.IsOperable
                                )
                            )
                            {
                                if (
                                    Convert.ToInt32(
                                        pnpDevice.GetProperty<byte>(
                                            WindowsPnPInformation.Device.BatteryPercentage
                                        )
                                    )
                                        is int batteryPercentage
                                    && batteryPercentage > 0
                                )
                                {
                                    var deviceEvent = new DeviceEvent()
                                    {
                                        Action = EventAction.Updated,
                                    };

                                    var error = deviceEvent.MergeBatteryInformation(
                                        pnpDevice.DeviceId,
                                        batteryPercentage
                                    );

                                    if (error == Errors.ErrorNone)
                                    {
                                        Output.Event(deviceEvent, token);
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            );

            watcher.Start();
            _isBatteryWatcherRunning = true;

            token.Wait();

            watcher.Stop();
        }
        finally
        {
            _isBatteryWatcherRunning = false;
        }
    }

    internal static void SetupDevicesWatcher(OperationToken token)
    {
        if (_isDeviceWatcherRunning)
            return;

        try
        {
            if (token.CancelTokenSource.IsCancellationRequested)
                return;

            using var monitor = new Monitor(
                BluetoothDevice.GetDeviceSelectorFromPairingState(true)
            );

            using var subscriber = new Monitor.Subscriber(monitor, token)
            {
                OnAdded = static (info, tok) =>
                {
                    if (info.OptionPaired.HasValue && info.OptionPaired.Value)
                    {
                        info.RefreshInformation();
                        Output.Event(info, tok);
                    }
                },
                OnRemoved = static (info, tok) =>
                {
                    Output.Event(info, tok);
                },
                OnUpdated = static (oldInfo, newInfo, tok) =>
                {
                    var wasPaired = oldInfo.OptionPaired.HasValue && oldInfo.OptionPaired.Value;
                    var isPaired = newInfo.OptionPaired.HasValue && newInfo.OptionPaired.Value;

                    if (isPaired)
                        Output.Event(newInfo, tok);
                },
            };

            monitor.Start();
            token.Wait();
        }
        finally
        {
            _isDeviceWatcherRunning = false;
        }
    }
}
