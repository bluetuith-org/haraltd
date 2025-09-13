using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.OperationToken;

namespace Haraltd.Stack.Base.Monitors;

public abstract class MonitorObserver
{
    public virtual void Initialize(OperationToken token) { }

    public virtual void StopAndRelease() { }

    public virtual void AdapterEventHandler(AdapterEvent adapterEvent) { }

    public virtual void DeviceEventHandler(DeviceEvent deviceEvent) { }
}
