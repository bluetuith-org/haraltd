using Bluetuith.Shim.DataTypes.Generic;
using Bluetuith.Shim.DataTypes.Models;
using Bluetuith.Shim.Stack.Microsoft.Windows;
using Nefarius.Utilities.Bluetooth;
using Nefarius.Utilities.DeviceManagement.PnP;

namespace Bluetuith.Shim.Stack.Microsoft.Devices.ConnectionMethods;

internal static class PnPDeviceStates
{
    internal static ErrorData Toggle(DeviceModel device)
    {
        try
        {
            var pnpDevices = new List<PnPDevice>();
            var aepIdProperty = WindowsPnPInformation.Device.AepId;

            foreach (var profile in device.OptionUuiDs ?? [])
            {
                var instances = 0;
                while (
                    Devcon.FindByInterfaceGuid(
                        profile,
                        out var pnpDevice,
                        instances++,
                        HostRadio.IsOperable
                    )
                )
                {
                    var aepId = pnpDevice.GetProperty<string>(aepIdProperty);
                    if (!DeviceUtils.ParseAepId(aepId, out _, out var deviceAddress))
                        continue;

                    if (device.Address == deviceAddress)
                        pnpDevices.Add(pnpDevice);
                }
            }

            foreach (var pnpDevice in pnpDevices)
                pnpDevice.Disable();
            foreach (var pnpDevice in pnpDevices)
                pnpDevice.Enable();
        }
        catch (Exception e)
        {
            return Errors.ErrorDeviceNotConnected.AddMetadata("exception", e.Message);
        }

        return Errors.ErrorNone;
    }
}