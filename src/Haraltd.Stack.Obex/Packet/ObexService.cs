namespace Haraltd.Stack.Obex.Packet;

public record ObexService(Guid Id, byte[] Value)
{
    public Guid ServiceGuid => Id;
    public byte[] ServiceUuid => Value ?? throw new ArgumentNullException(nameof(Value));
}
