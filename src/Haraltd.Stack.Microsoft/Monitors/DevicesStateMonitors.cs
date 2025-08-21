﻿using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.OutputStream;
using Haraltd.Stack.Microsoft.Devices;
using Haraltd.Stack.Microsoft.Windows;
using Nefarius.Utilities.Bluetooth;
using Nefarius.Utilities.DeviceManagement.PnP;
using Windows.Devices.Bluetooth;
using WmiLight;
using static Haraltd.DataTypes.Generic.IEvent;

namespace Haraltd.Stack.Microsoft.Monitors;

internal partial class DevicesStateMonitor : IMonitor
{
    private DevicesWatcher _monitor;
    private DevicesWatcher.Subscriber _subscriber;

    MonitorName IMonitor.Name => MonitorName.DevicesStateMonitor;
    bool IMonitor.RestartOnPowerStateChanged => false;
    bool IWatcher.IsRunning => _monitor is { IsRunning: true };
    bool IWatcher.IsCreated => _monitor != null;

    public void Create(OperationToken token)
    {
        _monitor = new DevicesWatcher(BluetoothDevice.GetDeviceSelectorFromPairingState(true));

        _subscriber = new DevicesWatcher.Subscriber(_monitor, token)
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
            OnUpdated = static (_, newInfo, tok) =>
            {
                var isPaired = newInfo.OptionPaired.HasValue && newInfo.OptionPaired.Value;

                if (isPaired)
                    Output.Event(newInfo, tok);
            },
        };
    }

    public bool Start()
    {
        return _monitor != null && _monitor.Start();
    }

    public void Stop()
    {
        _monitor?.Stop();
    }

    public void Dispose()
    {
        _subscriber?.Dispose();
        _monitor?.Dispose();
    }
}

internal partial class DevicesBatteryStateMonitor : IMonitor
{
    private RegistryWatcher _monitor;
    private OperationToken _token;

    MonitorName IMonitor.Name => MonitorName.DevicesBatteryMonitor;
    bool IMonitor.RestartOnPowerStateChanged => false;
    bool IWatcher.IsRunning => _monitor is { IsRunning: true };
    bool IWatcher.IsCreated => _monitor != null;

    public void Create(OperationToken token)
    {
        _token = token;
        _monitor = new RegistryWatcher(
            "HKEY_LOCAL_MACHINE",
            WindowsPnPInformation.HfEnumeratorRegistryKey,
            EventWatcher_EventArrived
        );
    }

    public bool Start()
    {
        return _monitor != null && _monitor.Start();
    }

    public void Stop()
    {
        _monitor?.Stop();
    }

    public void Dispose()
    {
        _monitor?.Dispose();
    }

    private void EventWatcher_EventArrived(object s, WmiEventArrivedEventArgs e)
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
                        is int batteryPercentage
                            and > 0
                    )
                    {
                        var deviceEvent = new DeviceEvent { Action = EventAction.Updated };

                        var error = deviceEvent.MergeBatteryInformation(
                            pnpDevice.DeviceId,
                            batteryPercentage
                        );

                        if (error == Errors.ErrorNone)
                            Output.Event(deviceEvent, _token);
                    }
            }
        }
        catch { }
    }
}
