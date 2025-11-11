using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Stack.Platform.Windows.Monitors;
using Haraltd.Stack.Platform.Windows.PlatformSpecific;
using InTheHand.Net;
using Nefarius.Utilities.Bluetooth;
using Nefarius.Utilities.DeviceManagement.PnP;

namespace Haraltd.Stack.Platform.Windows.Adapters;

internal static class WindowsAdapterExtensions
{
    public static ErrorData MergeRadioData<T>(this T adapter, OperationToken token = default)
        where T : notnull, IAdapter
    {
        return adapter.MergePnpDevice(false, false, token);
    }

    public static ErrorData MergeRadioEventData<T>(this T adapter, OperationToken token = default)
        where T : notnull, IAdapter
    {
        return adapter.MergePnpDevice(true, false, token);
    }

    public static AdapterEvent GetEmptyAdapterEventData(
        out ErrorData error,
        OperationToken token = default
    )
    {
        var adapterEventData = new AdapterEvent();
        error = adapterEventData.MergePnpDevice(true, true, token);

        return adapterEventData;
    }

    public static void MergeWatcherData<T>(
        this T adapter,
        ulong address,
        bool? powered,
        bool? discoverable
    )
        where T : notnull, IAdapter
    {
        adapter.Address = ((BluetoothAddress)address).ToString("C");
        adapter.OptionPowered = adapter.OptionPairable = powered;
        adapter.OptionDiscoverable = discoverable;
    }

    public static ErrorData MergePnpDevice<T>(
        this T adapter,
        bool eventOnly,
        bool addressOnly,
        OperationToken token = default
    )
        where T : notnull, IAdapter
    {
        try
        {
            if (!Devcon.FindByInterfaceGuid(HostRadio.DeviceInterface, out var device))
            {
                return Errors.ErrorAdapterNotFound;
            }

            var addr = (BluetoothAddress)
                device.GetProperty<ulong>(WindowsPnPInformation.Adapter.Address);
            adapter.Address = (addr).ToString("C");

            if (addressOnly)
                return Errors.ErrorNone;

            adapter.OptionPowered = adapter.OptionPairable = HostRadio.IsOperable;
            adapter.OptionDiscoverable = WindowsAdapter.GetDiscoverableState();

            if (token != default)
                adapter.OptionDiscovering = DiscoveryWatcher.IsStarted(token);

            if (eventOnly)
                return Errors.ErrorNone;

            adapter.Name =
                adapter.Alias =
                adapter.UniqueName =
                    device.GetProperty<string>(DevicePropertyKey.NAME);
            adapter.UuiDs = WindowsAdapter.GetAdapterServices(false);
        }
        catch (Exception e)
        {
            return Errors.ErrorAdapterNotFound.AddMetadata("exception", e.Message);
        }

        return Errors.ErrorNone;
    }
}
