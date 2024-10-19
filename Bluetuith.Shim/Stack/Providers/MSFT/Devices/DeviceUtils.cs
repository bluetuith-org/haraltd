using Bluetuith.Shim.Stack.Providers.MSFT.Adapters;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;

namespace Bluetuith.Shim.Stack.Providers.MSFT.Devices;

internal sealed class DeviceUtils
{
    public static async Task<BluetoothDevice> GetBluetoothDeviceWithService(string address, Guid service)
    {
        BluetoothDevice device = await GetBluetoothDevice(address);
        if (!device.DeviceInformation.Pairing.IsPaired)
            throw new ArgumentException($"Device {address} is not paired");

        RfcommDeviceServicesResult? result = await device.GetRfcommServicesForIdAsync(RfcommServiceId.FromUuid(service));
        return result is null || result.Services.Count == 0
            ? throw new ArgumentException($"The service ({BluetoothService.GetName(service)}) does not exist for device with address {address}")
            : device;
    }

    public static async Task<BluetoothDevice> GetBluetoothDevice(string address)
    {
        AdapterMethods.ThrowIfRadioNotOperable();

        BluetoothDevice? device = await BluetoothDevice.FromBluetoothAddressAsync(BluetoothAddress.Parse(address));
        return device is null ? throw new ArgumentNullException($"The device with address {address} was not found.") : device;
    }

    public static async Task<string> GetDeviceIdBySelector(string address, string selector)
    {
        var deviceId = "";
        ulong addressAsUlong = BluetoothAddress.Parse(address);

        foreach (DeviceInformation? deviceInfo in await DeviceInformation.FindAllAsync(AudioPlaybackConnection.GetDeviceSelector()))
        {
            BluetoothDevice bluetoothDevice = await BluetoothDevice.FromIdAsync(deviceInfo.Id);
            if (bluetoothDevice.BluetoothAddress == addressAsUlong)
            {
                deviceId = deviceInfo.Id;
            }
        }

        return string.IsNullOrEmpty(deviceId)
            ? throw new ArgumentException($"The device with address {address} was not found with selector {selector}")
            : deviceId;
    }

    public static string HostAddress(string address)
    {
        return string.IsNullOrEmpty(address) ? "" : address.Replace("(", "").Replace(")", "");
    }
}
