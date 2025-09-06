using System.Text;
using Haraltd.Stack.Base.Sockets;
using Haraltd.Stack.Obex.Streams;
using Haraltd.Stack.Obex.Utilities;

namespace Haraltd.Stack.Obex.Headers;

public class ObexHeader
{
    public ObexHeader(HeaderId headerId, byte[] buffer)
    {
        if (buffer.Length + sizeof(HeaderId) + sizeof(ushort) > ushort.MaxValue)
            throw new InvalidOperationException("Buffer size too large.");
        HeaderId = headerId;
        Buffer = buffer;
    }

    public ObexHeader(HeaderId headerId, byte b)
        : this(headerId, [b]) { }

    public ObexHeader(HeaderId headerId, int i)
        : this(headerId, i.ToBigEndianBytes()) { }

    public ObexHeader(HeaderId headerId, string text, bool nullTerminated, Encoding stringEncoding)
        : this(headerId, text.ToBytes(stringEncoding, nullTerminated)) { }

    public HeaderId HeaderId { get; }

    public ushort BufferLength => (ushort)Buffer.Length;

    public ushort HeaderTotalLength => (ushort)(BufferLength + sizeof(HeaderId) + sizeof(ushort));

    public byte[] Buffer { get; set; }

    public ObexHeaderEncoding Encoding => GetEncodingFromHeaderId(HeaderId);

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

    public string GetValueAsUtf8String(bool stringIsNullTerminated)
    {
        return GetValue(new StringInterpreter(System.Text.Encoding.UTF8, stringIsNullTerminated));
    }

    public string GetValueAsUnicodeString(bool stringIsNullTerminated)
    {
        return GetValue(
            new StringInterpreter(System.Text.Encoding.BigEndianUnicode, stringIsNullTerminated)
        );
    }

    public int GetValueAsInt32()
    {
        return GetValue(new Int32Interpreter());
    }

    public AppParameterDictionary GetValueAsAppParameters()
    {
        return GetValue<AppParameterHeaderInterpreter, AppParameterDictionary>();
    }

    private int GetLengthOrWrite(ISocketStreamWriter writer, bool write)
    {
        var len = 1 + Buffer.Length;
        var writeTotalLength = false;

        switch (Encoding)
        {
            case ObexHeaderEncoding.UnicodeString:
            case ObexHeaderEncoding.ByteSequence:
                len += sizeof(ushort);
                writeTotalLength = true;

                break;
        }

        if (write)
        {
            writer.WriteByte((byte)HeaderId);
            if (writeTotalLength)
                writer.WriteUInt16(HeaderTotalLength);
            writer.WriteBytes(Buffer);
        }

        return len;
    }

    public void WriteToStream(ISocketStreamWriter writer)
    {
        GetLengthOrWrite(writer, true);
    }

    public int GetVariableLength()
    {
        return GetLengthOrWrite(null!, false);
    }

    public static ObexHeader ReadFromStream(ISocketStreamReader reader)
    {
        var headerId = (HeaderId)reader.ReadByte();
        var encoding = GetEncodingFromHeaderId(headerId);
        ushort len;
        switch (encoding)
        {
            case ObexHeaderEncoding.UnicodeString:
            case ObexHeaderEncoding.ByteSequence:
                len = reader.ReadUInt16();
                const ushort prefix = sizeof(HeaderId) + sizeof(ushort);
                if (len < prefix)
                    throw new ObexException("Malformed header length.");
                len -= prefix;
                break;
            case ObexHeaderEncoding.OneByteQuantity:
                len = 1;
                break;
            case ObexHeaderEncoding.FourByteQuantity:
                len = 4;
                break;
            default:
                throw new InvalidOperationException("Unreachable code reached!");
        }

        var buffer = new byte[len];
        reader.ReadBytes(buffer);
        return new ObexHeader(headerId, buffer);
    }

    public static ObexHeaderEncoding GetEncodingFromHeaderId(HeaderId headerId)
    {
        return (ObexHeaderEncoding)((byte)headerId >> 6);
    }

    public override bool Equals(object obj)
    {
        return obj is ObexHeader header
            && HeaderId == header.HeaderId
            && ByteArrayEqualityComparer.Default.Equals(Buffer, header.Buffer);
    }

    public override int GetHashCode()
    {
        var hashCode = -310111661;
        hashCode = hashCode * -1521134295 + HeaderId.GetHashCode();
        hashCode = hashCode * -1521134295 + ByteArrayEqualityComparer.Default.GetHashCode(Buffer);
        return hashCode;
    }
}

public enum ObexHeaderEncoding : byte
{
    UnicodeString = 0,
    ByteSequence,
    OneByteQuantity,
    FourByteQuantity,
}
