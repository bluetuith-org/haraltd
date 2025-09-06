using Haraltd.DataTypes.Generic;
using InTheHand.Net;

namespace Haraltd.Stack.Base.Sockets;

public interface ISocket : IDisposable
{
    public BluetoothAddress Address { get; }

    public ISocketStreamReader Reader { get; }
    public ISocketStreamWriter Writer { get; }

    public Task<ErrorData> ConnectAsync();
}
