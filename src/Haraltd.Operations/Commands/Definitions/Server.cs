using ConsoleAppFramework;
using Haraltd.DataTypes.Generic;
using Haraltd.Operations.OutputStream;

namespace Haraltd.Operations.Commands.Definitions;

/// <summary>Manage new/existing RPC server services.</summary>
[RegisterCommands("server")]
public class Server
{
    /// <summary>Start a daemon server instance which will open a UNIX socket to perform RPC related functions.</summary>
    /// <param name="socketPath">-s, The full path and the socket name to be created and listened on.</param>
    /// <param name="tray">-t, Tray</param>
    // ReSharper disable once InvalidXmlDocComment
    public int Start(ConsoleAppContext context, string socketPath = "", bool tray = false)
    {
        if (Output.IsOnSocket)
            return Errors.ErrorUnsupported.Code.Value;

        if (socketPath == "")
            socketPath = OperationHost.Instance.Server.SocketPath;

        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var error = OperationHost.Instance.Server.StartServer(
            parserContext!.Token,
            socketPath,
            tray ? "" : "server start -t"
        );
        if (error != Errors.ErrorNone)
            Output.ErrorWithContext(error, parserContext!.Token, parserContext);

        return error.Code.Value;
    }

    /// <summary>Stop all daemon server instances in the current user session.</summary>
    /// <param name="tray">-t, Tray</param>
    // ReSharper disable once InvalidXmlDocComment
    public int Stop(ConsoleAppContext context, bool tray = false)
    {
        if (Output.IsOnSocket)
            return Errors.ErrorUnsupported.Code.Value;

        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var error = OperationHost.Instance.Server.StopServer(
            tray ? "" : string.Join(" ", context.Arguments) + " -t"
        );
        if (error != Errors.ErrorNone)
            Output.ErrorWithContext(error, parserContext!.Token, parserContext);

        return error.Code.Value;
    }
}
