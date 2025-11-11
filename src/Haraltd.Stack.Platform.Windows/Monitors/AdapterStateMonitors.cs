using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.OutputStream;
using Haraltd.Stack.Base.Monitors;

namespace Haraltd.Stack.Platform.Windows.Monitors;

internal partial class AdapterStateMonitors : MonitorObserver
{
    public override void AdapterEventHandler(AdapterEvent adapterEvent)
    {
        Output.Event(adapterEvent, OperationToken.None);
    }
}
