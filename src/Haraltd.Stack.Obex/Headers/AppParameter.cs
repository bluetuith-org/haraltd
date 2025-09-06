using Haraltd.Stack.Obex.Streams;
using Haraltd.Stack.Obex.Utilities;

namespace Haraltd.Stack.Obex.Headers;

public class AppParameter
{
    public AppParameter(byte tagId, byte[] buf)
    {
        if (buf.Length + 2 * sizeof(byte) > byte.MaxValue)
            throw new InvalidOperationException("Content buffer size too large. Max 126 bytes.");

        TagId = tagId;
        Buffer = buf;
    }

    public AppParameter(byte tagId, byte b)
        : this(tagId, b.ToBigEndianBytes()) { }

    public AppParameter(byte tagId, ushort value)
        : this(tagId, value.ToBigEndianBytes()) { }

    public AppParameter(byte tagId, int value)
        : this(tagId, value.ToBigEndianBytes()) { }

    public byte TagId { get; }

    public byte ContentLength => (byte)Buffer.Length;

    public byte TotalLength => (byte)(ContentLength + 2 * sizeof(byte));

    public byte[] Buffer { get; }

    public TR GetValue<TI, TR>()
        where TI : IBufferContentInterpreter<TR>, new()
    {
        var interpreter = new TI();
        return GetValue(interpreter);
    }

    public T GetValue<T>(IBufferContentInterpreter<T> interpreter)
    {
        return interpreter.GetValue(Buffer);
    }

    public ushort GetValueAsUInt16()
    {
        return GetValue<UInt16Interpreter, ushort>();
    }

    public override bool Equals(object obj)
    {
        return obj is AppParameter parameter
            && TagId == parameter.TagId
            && ByteArrayEqualityComparer.Default.Equals(Buffer, parameter.Buffer);
    }

    public override int GetHashCode()
    {
        var hashCode = -913184727;
        hashCode = hashCode * -1521134295 + TagId.GetHashCode();
        hashCode = hashCode * -1521134295 + ByteArrayEqualityComparer.Default.GetHashCode(Buffer);
        return hashCode;
    }
}
