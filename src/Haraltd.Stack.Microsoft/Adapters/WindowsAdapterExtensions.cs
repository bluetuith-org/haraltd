using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Stack.Microsoft.Monitors;
using Haraltd.Stack.Microsoft.Windows;
using InTheHand.Net;
using Nefarius.Utilities.Bluetooth;
using Nefarius.Utilities.DeviceManagement.PnP;

namespace Haraltd.Stack.Microsoft.Adapters;

internal static class WindowsAdapterExtensions
{
    public static ErrorData MergeRadioData<T>(this T adapter, OperationToken token = default)
        where T : notnull, IAdapter
    {
        return adapter.MergePnpDevice(false, token);
    }

    public static ErrorData MergeRadioEventData<T>(this T adapter, OperationToken token = default)
        where T : notnull, IAdapter
    {
        return adapter.MergePnpDevice(true, token);
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
        OperationToken token = default
    )
        where T : notnull, IAdapter
    {
        try
        {
            if (Devcon.FindByInterfaceGuid(HostRadio.DeviceInterface, out var device))
            {
                var addr = (BluetoothAddress)
                    device.GetProperty<ulong>(WindowsPnPInformation.Adapter.Address);
                adapter.Address = (addr).ToString("C");

                adapter.OptionPowered = adapter.OptionPairable = HostRadio.IsOperable;
                adapter.OptionDiscoverable = WindowsAdapter.GetDiscoverableState();

                if (token != default)
                    adapter.OptionDiscovering = DiscoveryWatcher.IsStarted(token);

                if (!eventOnly)
                {
                    adapter.Name =
                        adapter.Alias =
                        adapter.UniqueName =
                            device.GetProperty<string>(DevicePropertyKey.NAME);
                    adapter.UuiDs = WindowsAdapter.GetAdapterServices(false);
                }
            }
            else
            {
                WindowsAdapter.ThrowIfRadioNotOperable();
            }
        }
        catch (Exception e)
        {
            return Errors.ErrorAdapterNotFound.AddMetadata("exception", e.Message);
        }

        return Errors.ErrorNone;
    }
}
