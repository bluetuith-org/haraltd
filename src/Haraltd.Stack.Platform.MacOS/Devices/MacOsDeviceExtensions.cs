using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using IOBluetooth;

namespace Haraltd.Stack.Platform.MacOS.Devices;

public static class MacOsDeviceExtensions
{
    public static (DeviceModel, ErrorData) ToDeviceModel(this BluetoothDevice device)
    {
        try
        {
            var services = device.Services;

            var serviceUUIDs = new Guid[services.Length];
            var count = 0;

            foreach (var service in services)
            {
                if (SdpParser.GetServiceFromSdpRecord(service, out var serviceUUID))
                {
                    serviceUUIDs[count++] = serviceUUID;
                }
            }

            if (!AdapterNativeMethods.GetHostControllerAddress(out var hostControllerAddress))
            {
                var controller = HostController.DefaultController;
                if (controller != null)
                    hostControllerAddress = controller.AddressAsString;
            }

            return (
                new DeviceModel
                {
                    Address = device.AddressString.Replace("-", ":"),
                    AssociatedAdapter = hostControllerAddress,
                    OptionConnected = device.IsConnected,
                    OptionPaired = device.IsPaired,
                    OptionBlocked = false,
                    OptionBonded = device.IsPaired,
                    OptionRssi = device.Rssi,
                    OptionPercentage = device.BatteryPercentage,
                    OptionUuiDs = serviceUUIDs.ToArray(),
                    Name = device.Name,
                    Alias = device.Name,
                    Class = device.ClassOfDevice,
                    LegacyPairing = false,
                },
                Errors.ErrorNone
            );
        }
        catch (Exception e)
        {
            return (
                new DeviceModel(),
                Errors.ErrorDeviceNotFound.AddMetadata("exception", e.Message)
            );
        }
    }
}
