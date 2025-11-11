using Haraltd.Stack.Base.Sockets;
using Windows.Storage.Streams;

namespace Haraltd.Stack.Platform.Windows.Sockets;

public partial class WindowsSocketStreamReader(IInputStream inputStream) : ISocketStreamReader
{
    private readonly DataReader _reader = new(inputStream);

    public uint UnconsumedBufferLength => _reader.UnconsumedBufferLength;

    public async Task<uint> LoadAsync(uint length) => await _reader.LoadAsync(length);

    public byte ReadByte() => _reader.ReadByte();

    public void ReadBytes(byte[] value) => _reader.ReadBytes(value);

    public ulong ReadUInt64() => _reader.ReadUInt64();

    public ushort ReadUInt16() => _reader.ReadUInt16();

    public void Dispose()
    {
        _reader?.Dispose();
        inputStream?.Dispose();

        GC.SuppressFinalize(this);
    }
}
