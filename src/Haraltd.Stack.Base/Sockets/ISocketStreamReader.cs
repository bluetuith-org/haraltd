using DotNext;
using DotNext.Buffers;
using DotNext.IO;

namespace Haraltd.Stack.Base.Sockets;

public interface ISocketStreamReader : IDisposable
{
    public uint UnconsumedBufferLength { get; }

    public Task<uint> LoadAsync(uint length);

    public byte ReadByte();

    public void ReadBytes(byte[] value);

    public ulong ReadUInt64();

    public ushort ReadUInt16();
}
