using System.Buffers.Binary;
using DotNext.Buffers;
using DotNext.IO;
using Haraltd.Stack.Base.Sockets;
using IOBluetooth;

namespace Haraltd.Stack.Platform.MacOS.Sockets;

public class MacOsStreamWriter(RfcommChannel channel) : ISocketStreamWriter
{
    private readonly SparseBufferWriter<byte> _writer = new(8192 * 2);
    private readonly ManualResetEventSlim _writeEvent = new(true);

    public unsafe void WriteByte(byte value)
    {
        ThrowIfStoreRunning();

        var b = stackalloc byte[1];
        b[0] = value;

        var span = new Span<byte>(b, 1);
        _writer.Write(span);
    }

    public void WriteBytes(byte[] value)
    {
        ThrowIfStoreRunning();

        _writer.Write(value);
    }

    public void WriteInt64(long value)
    {
        return;
    }

    public unsafe void WriteUInt16(ushort value)
    {
        ThrowIfStoreRunning();

        var val = stackalloc byte[sizeof(ushort)];

        var span = new Span<byte>(val, sizeof(ushort));
        BinaryPrimitives.WriteUInt16BigEndian(span, value);

        _writer.Write(span);
    }

    public async Task StoreAsync()
    {
        ThrowIfStoreRunning();

        try
        {
            _writeEvent.Reset();
            DispatchBufferToSocket();
        }
        finally
        {
            _writeEvent.Set();
        }

        await ValueTask.CompletedTask;
    }

    private unsafe void DispatchBufferToSocket()
    {
        var result = 0;
        var written = 0;

        var len = _writer.WrittenCount;
        var totalLength = len;

        try
        {
            var reader = new SequenceReader(_writer.ToReadOnlySequence());

            while (result == 0 && len > 0)
            {
                if (written >= len)
                    break;

                var mtu = channel.Mtu;
                var bytesToSend = (len > mtu) ? mtu : len;

                var readonlySeq = reader.Read(bytesToSend);
                foreach (var readOnlyMemory in readonlySeq)
                {
                    var span = readOnlyMemory.Span;

                    fixed (byte* ptr = span)
                        result = channel.WriteSync(ptr, (ushort)span.Length);
                }

                len -= bytesToSend;
                written += (int)bytesToSend;
            }
        }
        catch (Exception e)
        {
            throw new EndOfStreamException($"The stream was unexpectedly closed: {e.Message}");
        }
        finally
        {
            _writer.Clear();
        }

        if (written != totalLength || result != 0)
            throw new EndOfStreamException("The entire data was not written to the stream");
    }

    private void ThrowIfStoreRunning()
    {
        if (!_writeEvent.Wait(100))
            throw new InvalidOperationException("StoreAsync is currently running");
    }

    public void Dispose()
    {
        _writer?.Dispose();
        _writeEvent?.Dispose();

        GC.SuppressFinalize(this);
    }
}
