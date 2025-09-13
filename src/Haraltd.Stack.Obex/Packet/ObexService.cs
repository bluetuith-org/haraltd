using Haraltd.Stack.Base.Sockets;
using InTheHand.Net;

namespace Haraltd.Stack.Obex.Packet;

public abstract record ObexService
{
    public abstract Guid ServiceGuid { get; }

    public abstract byte[] ServiceUuidBytes { get; }

    public abstract SocketOptions GetServiceSocketOptions(BluetoothAddress address);

    public abstract SocketListenerOptions GetServiceSocketListenerOptions();
}
