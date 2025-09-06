using System.Buffers.Binary;

namespace Haraltd.Stack.Obex.Utilities;

public static class IntExtensions
{
    public static byte[] ToBigEndianBytes(this int i)
    {
        var ret = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(ret, i);
        return ret;
    }

    public static byte[] ToBigEndianBytes(this ushort us)
    {
        var ret = new byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16BigEndian(ret, us);
        return ret;
    }

    public static byte[] ToBigEndianBytes(this byte b)
    {
        return [BinaryPrimitives.ReverseEndianness(b)];
    }
}
