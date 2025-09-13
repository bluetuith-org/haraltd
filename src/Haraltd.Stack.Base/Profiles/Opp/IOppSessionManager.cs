using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.OperationToken;
using InTheHand.Net;

namespace Haraltd.Stack.Base.Profiles.Opp;

public interface IOppSessionManager
{
    ValueTask<ErrorData> StartFileTransferSessionAsync(
        OperationToken token,
        BluetoothAddress address
    );

    (FileTransferModel, ErrorData) QueueFileSend(
        OperationToken token,
        BluetoothAddress address,
        string filepath
    );

    ValueTask<ErrorData> StopFileTransferSessionAsync(
        OperationToken token,
        BluetoothAddress address
    );

    ValueTask<ErrorData> CancelFileTransferSessionAsync(
        OperationToken token,
        BluetoothAddress address
    );

    ValueTask<ErrorData> StartFileTransferServerAsync(
        OperationToken token,
        BluetoothAddress adapterAddress,
        string destinationDirectory = ""
    );

    ValueTask<ErrorData> StopFileTransferServerAsync(
        OperationToken token,
        BluetoothAddress adapterAddress
    );
}
