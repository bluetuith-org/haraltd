using System.Buffers.Binary;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;

namespace IOBluetooth;

public static class BluetoothExtensions
{
    public static unsafe BluetoothDeviceAddress ToNativeAddress(this BluetoothAddress address)
    {
        var addrBytes = stackalloc byte[6];

        var addrSpan = new Span<byte>(addrBytes, 6);
        address.ToByteArray().AsSpan()[..6].CopyTo(addrSpan);

        for (var i = 0; i < 3; i++)
        {
            (addrBytes[i], addrBytes[5 - i]) = (addrBytes[5 - i], addrBytes[i]);
        }

        var nativeAddress = new BluetoothDeviceAddress();
        fixed (byte* p = addrSpan)
        {
            for (var i = 0; i < 6; i++)
            {
                nativeAddress.Data[i] = p[i];
            }
        }

        return nativeAddress;
    }

    public static BluetoothDeviceAddress ToNativeAddress(this string address)
    {
        var addr = address;
        if (addr.Contains('-'))
            addr = address.Replace("-", ":");

        return BluetoothAddress.Parse(addr).ToNativeAddress();
    }

    public static unsafe BluetoothPinCode ToNativePinCode(this uint numericValue, out uint length)
    {
        var pinCodeBytes = BitConverter.GetBytes(numericValue);
        length = (uint)pinCodeBytes.Length;

        var nativePinCode = new BluetoothPinCode();
        fixed (byte* p = pinCodeBytes)
        {
            for (var i = 0; i < pinCodeBytes.Length; i++)
            {
                nativePinCode.Data[i] = p[i];
            }
        }

        return nativePinCode;
    }

    public static unsafe SdpUuid ToNativeUuid(this Guid serviceUuid)
    {
        var uuidBytes = serviceUuid.ToByteArray(true);
        fixed (byte* uuidPtr = uuidBytes)
        {
            return SdpUuid.FromBytes(uuidPtr, (uint)(11 + 5));
        }
    }

    public static SdpUuid ToShortestNativeUuid(this Guid serviceUuid)
    {
        var arr = serviceUuid.ToByteArray(true).Take(sizeof(int)).ToArray();
        if (arr.Length != sizeof(int))
            return serviceUuid.ToNativeUuid();

        var uuid32 = BinaryPrimitives.ReadUInt32LittleEndian(arr);

        return SdpUuid.FromUuid32(uuid32);
    }
}
