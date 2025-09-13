namespace Haraltd.Stack.Base.Sockets;

public record SocketListenerOptions : SocketOptionsBase
{
    public required byte ChannelId;
}
