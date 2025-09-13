using Haraltd.Stack.Base.Sockets;
using Haraltd.Stack.Obex.Packet;
using InTheHand.Net;

namespace Haraltd.Stack.Obex.Profiles.Opp;

public record OppService : ObexService
{
    private const byte ChannelId = 0x0a;

    private static readonly Guid OppServiceUuid = new("00001105-0000-1000-8000-00805F9B34FB");

    public override Guid ServiceGuid => OppServiceUuid;
    public override byte[] ServiceUuidBytes { get; } = [];

    public override SocketOptions GetServiceSocketOptions(BluetoothAddress address)
    {
        return new SocketOptions { DeviceAddress = address, ServiceUuid = OppServiceUuid };
    }

    public override SocketListenerOptions GetServiceSocketListenerOptions()
    {
        return new SocketListenerOptions { ServiceUuid = OppServiceUuid, ChannelId = ChannelId };
    }
}
