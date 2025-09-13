using InTheHand.Net;

namespace Haraltd.Stack.Base.Sockets;

public record SocketOptions : SocketOptionsBase
{
    public required BluetoothAddress DeviceAddress;
}
