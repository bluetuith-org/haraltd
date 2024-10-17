using Bluetuith.Shim.Executor;
using Bluetuith.Shim.Types;
using System.Runtime.InteropServices;

namespace Bluetuith.Shim;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        using var sigintHandler = PosixSignalRegistration.Create(PosixSignal.SIGINT, SignalHandler);
        ErrorData error = await CommandExecutor.RunAsync(args);
        if (error != Errors.ErrorNone)
        {
            Console.Write(error.ToConsoleString());
        }
    }

    private static async void SignalHandler(PosixSignalContext context)
    {
        context.Cancel = true;
        await CommandExecutor.StopAsync();
    }
}
