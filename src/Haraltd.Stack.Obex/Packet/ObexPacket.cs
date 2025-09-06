using System.Text;
using Haraltd.Stack.Base.Sockets;
using Haraltd.Stack.Obex.Headers;
using Haraltd.Stack.Obex.Streams;
using Haraltd.Stack.Obex.Utilities;

namespace Haraltd.Stack.Obex.Packet;

public class ObexPacket
{
    private byte[] _bodyBuffer;

    public readonly Dictionary<HeaderId, ObexHeader> Headers;

    public ObexPacket()
        : this(new ObexOpcode(ObexOperation.Abort, true)) { }

    public ObexPacket(ObexOpcode op)
    {
        Opcode = op;
        Headers = new Dictionary<HeaderId, ObexHeader>();
    }

    public ObexPacket(ObexOpcode op, params ObexHeader[] headers)
        : this(op)
    {
        foreach (var h in headers)
            Headers[h.HeaderId] = h;
    }

    public ObexOpcode Opcode { get; set; }

    /// <summary>
    ///     Will only be updated after calling ToBuffer()
    /// </summary>
    public ushort PacketLength { get; private set; }

    public byte[] BodyBuffer
    {
        get => _bodyBuffer ?? throw new ObexException("Body does not exists");
        set => _bodyBuffer = value;
    }

    public ObexHeader GetHeader(HeaderId headerId)
    {
        if (Headers.TryGetValue(headerId, out var header))
            return header;

        throw new ObexHeaderNotFoundException(headerId);
    }

    public void AddHeader(ObexHeader header)
    {
        Headers.Add(header.HeaderId, header);
    }

    public void ReplaceHeader(HeaderId oldHeaderId, ObexHeader newHeader)
    {
        ArgumentNullException.ThrowIfNull(newHeader);

        Headers.Remove(oldHeaderId);
        Headers.Add(newHeader.HeaderId, newHeader);
    }

    public T GetBodyContent<T>(IBufferContentInterpreter<T> interpreter)
    {
        return interpreter.GetValue(BodyBuffer);
    }

    public string GetBodyContentAsUtf8String(bool stringIsNullTerminated)
    {
        return GetBodyContent(new StringInterpreter(Encoding.UTF8, stringIsNullTerminated));
    }

    public string GetBodyContentAsUnicodeString(bool stringIsNullTerminated)
    {
        return GetBodyContent(
            new StringInterpreter(Encoding.BigEndianUnicode, stringIsNullTerminated)
        );
    }

    protected virtual byte[] GetExtraField()
    {
        return null!;
    }

    /// <summary>
    ///     Read extra field after the length field
    /// </summary>
    /// <param name="reader"></param>
    /// <returns>The number of bits required for extra bits</returns>
    protected virtual Task<uint> ReadExtraField(ISocketStreamReader reader)
    {
        return Task.FromResult<uint>(0);
    }

    public void WriteToStream(ISocketStreamWriter writer)
    {
        PacketLength = sizeof(ObexOperation) + sizeof(ushort);

        var extra = GetExtraField();
        if (extra != null)
            PacketLength += (ushort)extra.Length;

        foreach (var header in Headers.Values)
            PacketLength += (ushort)header.GetVariableLength();

        writer.WriteByte(Opcode.Value);
        writer.WriteUInt16(PacketLength);

        if (extra != null)
            writer.WriteBytes(extra);

        foreach (var header in Headers.Values)
            header.WriteToStream(writer);
    }

    private async Task ParseHeader(ISocketStreamReader reader, uint headerSize)
    {
        if (headerSize <= 0)
            return;

        var sizeToRead = headerSize;
        while (sizeToRead > 0)
        {
            var loaded = await reader.LoadAsync(headerSize);
            if (loaded == 0)
                throw new ObexException(
                    "The underlying socket was closed before we were able to read the whole data."
                );
            sizeToRead -= loaded;
        }

        while (reader.UnconsumedBufferLength > 0)
        {
            var header = ObexHeader.ReadFromStream(reader);
            Headers[header.HeaderId] = header;
        }
    }

    public static Task<ObexPacket> ReadFromStream(ISocketStreamReader reader)
    {
        return ReadFromStream<ObexPacket>(reader);
    }

    /// <summary>
    ///     Read and parse OBEX packet from DataReader
    /// </summary>
    /// <param name="reader"></param>
    /// <returns>Loaded OBEX packet</returns>
    public static async Task<T> ReadFromStream<T>(ISocketStreamReader reader)
        where T : ObexPacket, new()
    {
        var loaded = await reader.LoadAsync(1);
        if (loaded != 1)
            throw new ObexException(
                "The underlying socket was closed before we were able to read the whole data."
            );

        ObexOpcode opcode = new(reader.ReadByte());
        var packet = new T();
        packet.Opcode = opcode;

        loaded = await reader.LoadAsync(sizeof(ushort));
        if (loaded != sizeof(ushort))
            throw new ObexException(
                "The underlying socket was closed before we were able to read the whole data."
            );

        packet.PacketLength = reader.ReadUInt16();

        var extraFieldBits = await packet.ReadExtraField(reader);
        var size =
            packet.PacketLength - (uint)sizeof(ObexOperation) - sizeof(ushort) - extraFieldBits;
        await packet.ParseHeader(reader, size);
        return packet;
    }

    public override bool Equals(object obj)
    {
        return obj is ObexPacket packet
            && EqualityComparer<ObexOpcode>.Default.Equals(Opcode, packet.Opcode)
            && PacketLength == packet.PacketLength
            && DictionaryEqualityComparer<HeaderId, ObexHeader>.Default.Equals(
                Headers,
                packet.Headers
            );
    }

    public override int GetHashCode()
    {
        var hashCode = 467068145;
        hashCode =
            hashCode * -1521134295 + EqualityComparer<ObexOpcode>.Default.GetHashCode(Opcode);
        hashCode = hashCode * -1521134295 + PacketLength.GetHashCode();
        hashCode =
            hashCode * -1521134295
            + EqualityComparer<Dictionary<HeaderId, ObexHeader>>.Default.GetHashCode(Headers);
        return hashCode;
    }
}
