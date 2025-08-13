using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.OperationToken;

namespace Haraltd.Stack.Microsoft.Windows;

internal record WindowsOppAuthEvent : OppAuthenticationEvent
{
    public WindowsOppAuthEvent(
        int timeout,
        IFileTransferEvent fileTransferEvent,
        OperationToken token
    )
        : base(timeout, fileTransferEvent, token) { }
}
