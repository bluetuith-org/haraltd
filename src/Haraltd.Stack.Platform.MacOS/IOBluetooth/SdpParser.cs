using System.Buffers.Binary;
using InTheHand.Net.Bluetooth;

namespace IOBluetooth;

public abstract class SdpParser
{
    public static unsafe bool GetServiceFromSdpRecord(NSObject? nvalue, out Guid service)
    {
        service = Guid.Empty;

        try
        {
            if (
                nvalue is not SdpServiceRecord record
                || record.GetAttributeDataElement(1) is not SdpDataElement element
            )
            {
                return false;
            }

            SdpUuid uuidptr = element.UuidValue;
            switch (element.TypeDescriptor)
            {
                case SdpDataElementType.DataElementSequence:
                    if (
                        element.ArrayValue is not SdpDataElement[] elemArray
                        || elemArray == null
                        || elemArray.Length == 0
                    )
                        return false;

                    uuidptr = elemArray[0].UuidValue;
                    goto case SdpDataElementType.Uuid;

                case SdpDataElementType.Uuid:
                    if (uuidptr == null)
                        return false;

                    var n = BinaryPrimitives.ReadUInt32BigEndian(
                        new Span<byte>(uuidptr.Bytes.ToPointer(), (int)uuidptr.Length)
                    );

                    service = BluetoothService.CreateBluetoothUuid(n);
                    break;
            }
        }
        catch
        {
            return false;
        }

        return true;
    }
}
