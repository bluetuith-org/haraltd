using Bluetuith.Shim.DataTypes.Generic;

namespace Bluetuith.Shim.Operations;

public interface IServer
{
    public const string Name = "bluetuith-shim";

    public static string SocketPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "bh-shim",
            "bh-shim.sock"
        );

    public bool IsAdministrator();
    public bool Relaunch();
    public ErrorData StartServer(string socketPath, string parameter);
    public ErrorData StopServer(string parameter);
}