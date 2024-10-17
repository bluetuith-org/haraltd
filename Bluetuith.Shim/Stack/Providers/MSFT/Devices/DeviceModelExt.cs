using Bluetuith.Shim.Stack.Models;
using Bluetuith.Shim.Stack.Providers.MSFT.Adapters;
using Bluetuith.Shim.Types;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using Windows.Devices.Bluetooth;

namespace Bluetuith.Shim.Stack.Providers.MSFT.Devices;

public record class DeviceModelExt : DeviceModel
{
    public DeviceModelExt(BluetoothDeviceInfo device)
    {
        Address = device.DeviceAddress.ToString("C");
        Name = device.DeviceName;
        Class = new DeviceClass
        {
            MajorDevice = (int)device.ClassOfDevice.MajorDevice,
            Device = (int)device.ClassOfDevice.Device,
            Service = (int)device.ClassOfDevice.Service,
        };
        Connected = device.Connected;
        Paired = device.Authenticated;
        UUIDs = [];
    }

    public DeviceModelExt(BluetoothDevice device)
    {
        Address = new BluetoothAddress(device.BluetoothAddress).ToString("C");
        Name = device.Name;

        var classOfDevice = (ClassOfDevice)device.ClassOfDevice.RawValue;
        Class = new DeviceClass
        {
            MajorDevice = (int)classOfDevice.MajorDevice,
            Device = (int)classOfDevice.Device,
            Service = (int)classOfDevice.Service,
        };

        Connected = device.ConnectionStatus == BluetoothConnectionStatus.Connected;
        Paired = device.DeviceInformation.Pairing.IsPaired;
        UUIDs = [];
    }

    public async Task<ErrorData> AppendServices()
    {
        try
        {
            BluetoothDeviceInfo device = new(BluetoothAddress.Parse(Address));

            var uuids = await Task.Run(() => device.GetL2CapServicesAsync());
            UUIDs = uuids.ToArray();
        }
        catch (Exception e)
        {
            return StackErrors.ErrorDeviceServicesNotFound.WrapError(new()
            {
                {"exception", e.Message }
            });
        }

        return UUIDs.Length == 0 ? StackErrors.ErrorDeviceServicesNotFound : Errors.ErrorNone;
    }

    public static DeviceModel ConvertToDeviceModel(BluetoothDeviceInfo device)
    {
        return new DeviceModelExt(device);
    }

    public static DeviceModel ConvertToDeviceModel(BluetoothDevice device)
    {
        return new DeviceModelExt(device);
    }

    public static async Task<(DeviceModel device, ErrorData error)> ConvertToDeviceModelAsync(
        string address,
        bool issueInquiryIfNotFound,
        bool appendServices,
        CancellationToken token = default
    )
    {
        DeviceModel device = new();

        try
        {
            BluetoothDeviceInfo? bluetoothDevice = await Client.Handle.DiscoverDeviceByAddressAsync(address, issueInquiryIfNotFound, token);
            if (bluetoothDevice is not null)
            {
                DeviceModelExt deviceExt = new(bluetoothDevice);
                if (appendServices)
                {
                    await deviceExt.AppendServices();
                }

                device = deviceExt;
            }
        }
        catch (Exception e)
        {
            return (device, StackErrors.ErrorDeviceNotFound.WrapError(new() {
                { "exception", e.Message }
            }));
        }

        return (device, Errors.ErrorNone);
    }
}