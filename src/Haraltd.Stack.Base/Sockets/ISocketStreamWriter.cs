using DotNext.Buffers;
using DotNext.IO;

namespace Haraltd.Stack.Base.Sockets;

public interface ISocketStreamWriter : IDisposable
{
    public void WriteByte(byte value);

    public void WriteBytes(byte[] value);

    public void WriteInt64(long value);

    public void WriteUInt16(ushort value);

    public Task StoreAsync();
}
