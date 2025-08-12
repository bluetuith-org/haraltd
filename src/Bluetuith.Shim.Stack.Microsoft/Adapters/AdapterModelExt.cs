using Bluetuith.Shim.DataTypes.Generic;
using Bluetuith.Shim.DataTypes.Models;
using Bluetuith.Shim.DataTypes.OperationToken;
using Bluetuith.Shim.Stack.Microsoft.Windows;
using InTheHand.Net;
using Nefarius.Utilities.Bluetooth;
using Nefarius.Utilities.DeviceManagement.PnP;

namespace Bluetuith.Shim.Stack.Microsoft.Adapters;

internal record AdapterModelExt : AdapterModel
{
    internal static string CurrentAddress
    {
        get
        {
            var address = "";

            if (Devcon.FindByInterfaceGuid(HostRadio.DeviceInterface, out var device))
                address = (
                    (BluetoothAddress)
                    device.GetProperty<ulong>(WindowsPnPInformation.Adapter.Address)
                ).ToString("C");
            else
                AdapterMethods.ThrowIfRadioNotOperable();

            return address;
        }
    }

    internal static (AdapterModel Adapter, ErrorData Error) ConvertToAdapterModel(
        OperationToken token = default
    )
    {
        AdapterModel adapter = new();

        return (adapter, adapter.MergeRadioData(token));
    }
}