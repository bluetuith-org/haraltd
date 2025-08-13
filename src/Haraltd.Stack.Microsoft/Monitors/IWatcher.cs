namespace Haraltd.Stack.Microsoft.Monitors;

internal interface IWatcher : IDisposable
{
    public bool IsRunning { get; }
    public bool IsCreated { get; }

    public bool Start();

    public void Stop();
}
