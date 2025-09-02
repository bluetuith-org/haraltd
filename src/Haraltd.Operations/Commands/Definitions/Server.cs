using ConsoleAppFramework;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.OutputStream;
using Haraltd.Stack;
using Haraltd.Stack.Base;

namespace Haraltd.Operations.Commands.Definitions;

/// <summary>Manage new/existing RPC server services.</summary>
[RegisterCommands("server")]
public class Server
{
    /// <summary>Start a daemon server instance which will open a UNIX socket to perform RPC related functions.</summary>
    /// <param name="socketPath">-s, The full path and the socket name to be created and listened on.</param>
    /// <param name="tray">-t, Tray</param>
    public int Start(ConsoleAppContext context, string socketPath = "", bool tray = false)
    {
        if (Output.IsOnSocket)
            return Errors.ErrorUnsupported.Code.Value;

        if (socketPath == "")
            socketPath = IServer.SocketPath;

        var error = OperationHost.Instance.Server.StartServer(
            socketPath,
            tray ? "" : "server start -t"
        );
        if (error != Errors.ErrorNone)
            Output.Error(error, OperationToken.None);

        return error.Code.Value;
    }

    /// <summary>Stop all daemon server instances in the current user session.</summary>
    public int Stop(ConsoleAppContext context, bool tray = false)
    {
        if (Output.IsOnSocket)
            return Errors.ErrorUnsupported.Code.Value;

        var error = OperationHost.Instance.Server.StopServer(
            tray ? "" : string.Join(" ", context.Arguments) + " -t"
        );
        if (error != Errors.ErrorNone)
            Output.Error(error, OperationToken.None);

        return error.Code.Value;
    }
}
