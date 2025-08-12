using Bluetuith.Shim.DataTypes.Events;
using Bluetuith.Shim.DataTypes.OperationToken;

namespace Bluetuith.Shim.Stack.Microsoft.Windows;

internal record WindowsOppAuthEvent : OppAuthenticationEvent
{
    public WindowsOppAuthEvent(
        int timeout,
        IFileTransferEvent fileTransferEvent,
        OperationToken token
    )
        : base(timeout, fileTransferEvent, token)
    {
    }
}