using System.Buffers.Binary;
using System.IO.Pipelines;
using DotNext.IO;
using Haraltd.Stack.Base.Sockets;

namespace Haraltd.Stack.Platform.MacOS.Sockets;

public class MacOsStreamReader(PipeReader reader) : ISocketStreamReader
{
    private readonly PoolingBufferedStream _bufferWriter = new(reader.AsStream(true))
    {
        MaxBufferSize = 8192 * 2,
    };

    public uint UnconsumedBufferLength => (uint)(_bufferWriter.HasBufferedDataToRead ? 1 : 0);

    public async Task<uint> LoadAsync(uint length)
    {
        if (_bufferWriter.HasBufferedDataToRead)
        {
            return length;
        }

        var res = await _bufferWriter.ReadAsync();
        return !res ? 0 : length;
    }

    public byte ReadByte()
    {
        var b = _bufferWriter.ReadByte();
        if (b == -1)
            throw new EndOfStreamException();

        return (byte)b;
    }

    public void ReadBytes(byte[] value)
    {
        ReadIntoSpan(value);
    }

    private void ReadIntoSpan(Span<byte> value)
    {
        _bufferWriter.ReadExactly(value);
    }

    public ulong ReadUInt64()
    {
        return 0;
    }

    public unsafe ushort ReadUInt16()
    {
        var buf = stackalloc byte[sizeof(ushort)];

        var span = new Span<byte>(buf, sizeof(ushort));
        ReadIntoSpan(span);

        return BinaryPrimitives.ReadUInt16BigEndian(span);
    }

    public void Dispose()
    {
        _bufferWriter.Dispose();
        GC.SuppressFinalize(this);
    }
}
