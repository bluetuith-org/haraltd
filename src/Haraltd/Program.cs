using System.Runtime.InteropServices;
using Haraltd.DataTypes.Generic;
using Haraltd.Operations;
using Haraltd.Operations.Managers;
using Haraltd.Stack.Base;
using Timer = System.Timers.Timer;

namespace Haraltd;

internal static class Program
{
    private static readonly IServer Server;

    static Program()
    {
        OperationHost.Register(new PlatformSpecificHost());
        Server = OperationHost.Instance.Server;
    }

    public static void Main(string[] args)
    {
        using var _ = Server;
        var token = OperationManager.GenerateToken(0, Guid.NewGuid());

        var error = Server.ShouldContinueExecution(args);
        PrintErrorAndExit(error);

        using var sigintHandler = PosixSignalRegistration.Create(PosixSignal.SIGINT, SignalHandler);

        if (Server.TryRelaunch(token, args, out error))
        {
            if (error != Errors.ErrorNone)
                PrintErrorAndExit(error);

            return;
        }

        error = Server.LaunchCurrentInstance(token, args);
        PrintErrorAndExit(error);
    }

    private static void PrintErrorAndExit(ErrorData error)
    {
        if (error == Errors.ErrorNone)
            return;

        Console.Write(error.ToConsoleString());
        Environment.Exit(error.Code.Value);
    }

    private static void SignalHandler(PosixSignalContext context)
    {
        var timer = new Timer(TimeSpan.FromSeconds(10));
        timer.Elapsed += (_, __) =>
        {
            Environment.Exit(1);
        };
        timer.AutoReset = false;

        timer.Start();

        context.Cancel = true;
        Server.StopCurrentInstance();

        timer.Stop();
    }
}
