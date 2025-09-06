using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using InTheHand.Net;

namespace Haraltd.Stack.Obex.Session;

public interface IObexSessions
{
    public ValueTask<ErrorData> StartAsyncByAddress(
        OperationToken token,
        BluetoothAddress address,
        IObexSession session
    );

    void RemoveSession(IObexSession session);

    public IEnumerable<T> EnumerateSessions<T>()
        where T : class, IObexSession;
}
