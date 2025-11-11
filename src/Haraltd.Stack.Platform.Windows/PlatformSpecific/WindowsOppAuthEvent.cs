using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.OperationToken;

namespace Haraltd.Stack.Platform.Windows.PlatformSpecific;

internal record WindowsOppAuthEvent : OppAuthenticationEvent
{
    public WindowsOppAuthEvent(int timeout, IFileTransfer fileTransferData, OperationToken token)
        : base(timeout, fileTransferData, token) { }
}
