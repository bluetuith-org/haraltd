using Haraltd.DataTypes.Generic;

namespace Haraltd.Stack.Base;

public interface IServer
{
    public const string Name = "haraltd";

    public static string SocketPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "haraltd",
            "hd.sock"
        );

    public bool IsAdministrator();
    public bool Relaunch();
    public ErrorData StartServer(string socketPath, string parameter);
    public ErrorData StopServer(string parameter);
}
