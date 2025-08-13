using Haraltd.Stack.Microsoft.Devices.Profiles;

namespace Haraltd.Stack.Microsoft.Monitors;

internal partial class OppServerMonitor : AdapterStateMonitor
{
    public override MonitorName Name => MonitorName.OppServerStateMonitor;

    public override bool RestartOnPowerStateChanged => true;

    public override bool IsRunning => IsCreated;

    public override bool IsCreated =>
        Radio != null && Opp.IsServerSessionExist(Token, AdapterAddress, out _);

    public override bool Start()
    {
        if (base.Start())
            Opp.StartFileTransferServerAsync(Token).GetAwaiter().GetResult();

        return IsCreated;
    }

    public override void Stop()
    {
        Opp.StopFileTransferServer(Token);
        base.Stop();
    }
}
