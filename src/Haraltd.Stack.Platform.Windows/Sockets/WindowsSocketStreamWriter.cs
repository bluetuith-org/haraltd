using System.Runtime.InteropServices.WindowsRuntime;
using Haraltd.Stack.Base.Sockets;
using Windows.Storage.Streams;

namespace Haraltd.Stack.Platform.Windows.Sockets;

public partial class WindowsSocketStreamWriter(IOutputStream outputStream) : ISocketStreamWriter
{
    private readonly DataWriter _writer = new(outputStream);

    public void WriteByte(byte value) => _writer.WriteByte(value);

    public void WriteBytes(byte[] value) => _writer.WriteBytes(value);

    public void WriteInt64(long value) => _writer.WriteInt64(value);

    public void WriteUInt16(ushort value) => _writer.WriteUInt16(value);

    public async Task StoreAsync()
    {
        await _writer.StoreAsync();
    }

    public void Dispose()
    {
        _writer?.Dispose();
        outputStream?.Dispose();

        GC.SuppressFinalize(this);
    }
}
