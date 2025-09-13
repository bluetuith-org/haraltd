using System.Runtime.CompilerServices;
using InTheHand.Net;

namespace IOBluetooth;

public partial class BluetoothDevice
{
    public string AddressToString => AddressString.Replace("-", ":").ToUpperInvariant();

    public unsafe BluetoothDeviceAddress GetAddress()
    {
        return Unsafe.AsRef<BluetoothDeviceAddress>(Address().ToPointer());
    }

    public BluetoothAddress? FormattedAddress =>
        !BluetoothAddress.TryParse(AddressToString, out var address) ? null : address;

    public static BluetoothDevice DeviceFromBluetoothAddress(BluetoothAddress address)
    {
        var addrString = address.ToString("C").Replace("-", ":");
        return DeviceWithAddressString(addrString);
    }
}
