using Haraltd.DataTypes.Generic;

namespace Haraltd.Stack.Base.Sockets;

public interface ISocketListener : IDisposable
{
    public event Action<ISocket> OnConnected;
    public ValueTask<ErrorData> StartAdvertisingAsync();
}
