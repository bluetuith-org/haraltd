using Haraltd.DataTypes.OperationToken;

namespace Haraltd.Stack.Microsoft.Monitors;

internal interface IMonitor : IWatcher
{
    public MonitorName Name { get; }
    public bool RestartOnPowerStateChanged { get; }

    public void Create(OperationToken token);
}

internal enum MonitorName : byte
{
    None = 0,
    RegistryMonitor = 1,
    DevicesMonitor = 2,
    DevicesStateMonitor = 3,
    DevicesBatteryMonitor = 4,
    AdapterPowerStateMonitor = 5,
    AdapterDiscoverableStateMonitor = 6,
    OppServerStateMonitor = 7,
}
