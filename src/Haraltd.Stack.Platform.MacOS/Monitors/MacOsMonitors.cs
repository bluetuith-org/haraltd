using System.Collections.Frozen;
using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Stack.Base.Monitors;
using IOBluetooth;
using IOBluetooth.Shim;

namespace Haraltd.Stack.Platform.MacOS.Monitors;

internal static class MacOsMonitors
{
    private static bool IsRunning { get; set; }

    private static string _currentAdapterAddress = "";

    internal static string CurrentAdapterAddress
    {
        get
        {
            lock (_currentAdapterAddress)
                return _currentAdapterAddress;
        }
        private set
        {
            lock (_currentAdapterAddress)
                _currentAdapterAddress = value;
        }
    }

    private static readonly FrozenSet<MonitorObserver> Monitors =
    [
        new AdapterStateMonitor(),
        new DeviceStateMonitor(),
        new OppServerMonitor(),
        DiscoveryMonitor.MonitorInstance,
    ];

    public static ErrorData Start(OperationToken token)
    {
        if (IsRunning)
            return Errors.ErrorNone;

        var res = BluetoothEvents.TryInitWithDelegate(new MacOsMonitorDelegate());
        if (res.Error != null)
        {
            Stop();
            return Errors.ErrorUnexpected.AddMetadata("exception", res.Error.UserInfo.Description);
        }

        try
        {
            UpdateHostControllerAddress();

            foreach (var monitor in Monitors)
            {
                monitor.Initialize(token);
            }
        }
        catch (Exception ex)
        {
            return Errors.ErrorUnexpected.AddMetadata("exception", ex.Message);
        }

        IsRunning = true;

        return Errors.ErrorNone;
    }

    public static ErrorData Stop()
    {
        if (!IsRunning)
            return Errors.ErrorNone;

        try
        {
            foreach (var monitor in Monitors)
                monitor.StopAndRelease();
        }
        catch
        {
            // ignored
        }

        IsRunning = false;
        return Errors.ErrorNone;
    }

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

    private static void UpdateHostControllerAddress()
    {
        AdapterNativeMethods.GetHostControllerAddress(out var hostControllerAddress);
        CurrentAdapterAddress = hostControllerAddress;
    }

    private class MacOsMonitorDelegate : BluetoothEventDelegate
    {
        public override void HandleAdapterEvent(AdapterEventData data)
        {
            var adapterEvent = new AdapterEvent();
            adapterEvent.AppendAdapterEventData(data, CurrentAdapterAddress);

            if (adapterEvent.OptionPowered is true)
            {
                UpdateHostControllerAddress();
                adapterEvent.Address = CurrentAdapterAddress;
            }

            PublishAdapterEvent(adapterEvent);
        }

        public override void HandleDeviceEvent(
            BluetoothDevice device,
            EventAction action,
            DeviceEventData data
        )
        {
            if (device == null)
                return;

            var deviceEvent = new DeviceEvent();
            deviceEvent.AppendDeviceEventData(
                device,
                (IEvent.EventAction)action,
                CurrentAdapterAddress,
                data
            );

            PublishDeviceEvent(deviceEvent);
        }
    }
}

internal static class MacOsMonitorEventExtensions
{
    internal static void AppendAdapterEventData(
        this AdapterEvent eventData,
        AdapterEventData dataToAppend,
        string hostControllerAddress
    )
    {
        eventData.Name = eventData.Address = HostController.DefaultController.NameAsString;
        eventData.Address = hostControllerAddress;
        eventData.Action = IEvent.EventAction.Updated;
        eventData.OptionPowered = dataToAppend.Powered == 0 ? null : dataToAppend.Powered > 4;
        eventData.OptionDiscoverable =
            dataToAppend.Discoverable == 0 ? null : dataToAppend.Discoverable > 4;
        eventData.OptionPairable = eventData.OptionPowered;
    }

    internal static void AppendDeviceEventData(
        this DeviceEvent eventData,
        BluetoothDevice dataToAppend,
        IEvent.EventAction action,
        string hostControllerAddress,
        DeviceEventData additionalData
    )
    {
        eventData.Action = action;

        eventData.Name = eventData.Alias = dataToAppend.Name;
        eventData.Class = dataToAppend.ClassOfDevice;
        eventData.Address = dataToAppend.AddressToString;
        eventData.AssociatedAdapter = hostControllerAddress;
        eventData.OptionConnected = dataToAppend.IsConnected;
        eventData.OptionPaired = dataToAppend.IsPaired;
        eventData.OptionBonded = eventData.OptionPaired;
        eventData.OptionRssi = dataToAppend.Rssi == 0 ? null : dataToAppend.Rssi;

        if (!additionalData.BatteryInfo.modified)
            return;

        eventData.OptionPercentage = additionalData.BatteryInfo.BatteryPercentSingle;
    }

    internal static DeviceEvent ToDeviceEvent(
        this BluetoothDevice device,
        IEvent.EventAction action,
        string hostControllerAddress
    )
    {
        var ev = new DeviceEvent();
        ev.AppendDeviceEventData(device, action, hostControllerAddress, default);

        return ev;
    }
}
