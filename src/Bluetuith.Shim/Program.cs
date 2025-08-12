using System.Runtime.InteropServices;
using Bluetuith.Shim.DataTypes.Generic;
using Bluetuith.Shim.Operations;
using Bluetuith.Shim.Operations.Commands;
using Bluetuith.Shim.Stack.Microsoft;
using Bluetuith.Shim.Stack.Microsoft.Server;

namespace Bluetuith.Shim;

internal static class Program
{
    public static void Main(string[] args)
    {
#if WINDOWS10_0_19041_0_OR_GREATER
        BluetoothStack.CurrentStack = new MsftStack();
        ServerHost.Instance = new MsftServer();
#endif

        ServerHost.Instance.Relaunch();

        using var sigintHandler = PosixSignalRegistration.Create(PosixSignal.SIGINT, SignalHandler);
        ErrorData error = CommandExecutor.Run(args);
        if (error != Errors.ErrorNone)
        {
            Console.Write(error.ToConsoleString());
            Environment.Exit(1);
        }
    }

    private static void SignalHandler(PosixSignalContext context)
    {
        context.Cancel = true;
        CommandExecutor.StopAsync().Wait();
    }
}