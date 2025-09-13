using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;

namespace Haraltd.Stack.Base;

public interface IServer : IDisposable
{
    public const string Name = "haraltd";
    public const string SocketName = "hd.sock";

    public bool IsRunning { get; }

    public string RootCacheDirectory { get; }
    public string AppCacheDirectory => Path.Combine(RootCacheDirectory, Name);
    public string SocketPath => Path.Combine(RootCacheDirectory, Name, SocketName);

    public bool TryRelaunch(OperationToken token, string[] args, out ErrorData err);
    public ErrorData ShouldContinueExecution(string[] args);
    public ErrorData LaunchCurrentInstance(OperationToken token, string[] args);
    public void StopCurrentInstance();

    public ErrorData StartServer(OperationToken token, string socketPath, string parameter);
    public ErrorData StopServer(string parameter);
}
