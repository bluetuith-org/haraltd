using System.Buffers.Binary;
using System.Text;

namespace Haraltd.Stack.Obex.Streams;

public interface IBufferContentInterpreter<out T>
{
    T GetValue(ReadOnlySpan<byte> buffer);
}

public class UInt16Interpreter : IBufferContentInterpreter<ushort>
{
    public ushort GetValue(ReadOnlySpan<byte> buffer)
    {
        return BinaryPrimitives.ReadUInt16BigEndian(buffer);
    }
}

public class Int32Interpreter : IBufferContentInterpreter<int>
{
    public int GetValue(ReadOnlySpan<byte> buffer)
    {
        return BinaryPrimitives.ReadInt32BigEndian(buffer);
    }
}

public class StringInterpreter(Encoding stringEncoding, bool stringIsNullTerminated)
    : IBufferContentInterpreter<string>
{
    private Encoding StringEncoding { get; } = stringEncoding;
    private bool StringIsNullTerminated { get; } = stringIsNullTerminated;

    public string GetValue(ReadOnlySpan<byte> buffer)
    {
        var len = buffer.Length;
        if (StringIsNullTerminated)
            len -= StringEncoding.GetByteCount("\0");
        return StringEncoding.GetString(buffer[..len]);
    }
}
