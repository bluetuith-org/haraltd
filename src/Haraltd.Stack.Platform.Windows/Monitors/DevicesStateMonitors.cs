using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.OutputStream;
using Haraltd.Stack.Base.Monitors;

namespace Haraltd.Stack.Platform.Windows.Monitors;

internal partial class DevicesStateMonitors : MonitorObserver
{
    public override void DeviceEventHandler(DeviceEvent deviceEvent)
    {
        Output.Event(deviceEvent, OperationToken.None);
    }
}
