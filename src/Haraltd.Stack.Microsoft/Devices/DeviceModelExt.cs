using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Nefarius.Utilities.DeviceManagement.PnP;

namespace Haraltd.Stack.Microsoft.Devices;

internal record DeviceModelExt : DeviceModel
{
    private DeviceModelExt() { }

    internal static (DeviceModel, ErrorData) ConvertToDeviceModel(PnPDevice pnpDevice)
    {
        DeviceModelExt device = new();

        return (device, device.MergePnpDevice(pnpDevice));
    }

    internal static (DeviceModel, ErrorData) ConvertToDeviceModel(string address)
    {
        DeviceModel device = new();

        return (device, device.MergeDeviceByAddress(address));
    }
}
