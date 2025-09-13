using Haraltd.DataTypes.Generic;
using InTheHand.Net;

namespace Haraltd.Stack.Base.Sockets;

public interface ISocket : IDisposable
{
    public BluetoothAddress Address { get; }

    public int Mtu { get; }

    public event Action<bool> ConnectionStatusEvent;
    public bool CanSubscribeToEvents { get; }

    public ISocketStreamReader Reader { get; }
    public ISocketStreamWriter Writer { get; }

    public Task<ErrorData> ConnectAsync();
}
