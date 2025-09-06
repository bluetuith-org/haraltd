using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Stack.Base;
using InTheHand.Net;

namespace Haraltd.Stack.Obex.Session;

public interface IObexSession : IDisposable
{
    public BluetoothAddress Address { get; set; }
    public OperationToken Token { get; set; }
    public IBluetoothStack Stack { get; set; }
    public IObexSessions Sessions { get; set; }

    public ValueTask<ErrorData> StartAsync();
    public ValueTask<ErrorData> StopAsync();

    public ValueTask<ErrorData> CancelTransferAsync();
}
