using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using InTheHand.Net;

namespace Haraltd.Stack.Base.Profiles.Opp;

public readonly record struct AdapterOppManager(
    IOppSessionManager SessionManager,
    OperationToken Token,
    BluetoothAddress Address
)
{
    public ValueTask<ErrorData> StartFileTransferServerAsync(string destinationDirectory = "") =>
        SessionManager.StartFileTransferServerAsync(Token, Address, destinationDirectory);

    public ValueTask<ErrorData> StopFileTransferServerAsync() =>
        SessionManager.StopFileTransferServerAsync(Token, Address);
}
