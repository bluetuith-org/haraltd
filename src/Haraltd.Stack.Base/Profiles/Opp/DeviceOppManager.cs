using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.OperationToken;
using InTheHand.Net;

namespace Haraltd.Stack.Base.Profiles.Opp;

public readonly record struct DeviceOppManager(
    IOppSessionManager SessionManager,
    OperationToken Token,
    BluetoothAddress Address
)
{
    public ValueTask<ErrorData> StartFileTransferSessionAsync() =>
        SessionManager.StartFileTransferSessionAsync(Token, Address);

    public (FileTransferModel, ErrorData) QueueFileSend(string filepath) =>
        SessionManager.QueueFileSend(Token, Address, filepath);

    public ValueTask<ErrorData> StopFileTransferSessionAsync() =>
        SessionManager.StopFileTransferSessionAsync(Token, Address);

    public ValueTask<ErrorData> CancelFileTransferSessionAsync() =>
        SessionManager.CancelFileTransferSessionAsync(Token, Address);
}
