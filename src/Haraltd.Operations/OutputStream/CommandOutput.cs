using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.Managers;

namespace Haraltd.Operations.OutputStream;

internal sealed class CommandOutput : OutputBase
{
    internal override int EmitResult<T>(T result, OperationToken token)
    {
        Console.Write(result.ToConsoleString());
        return base.EmitResult(result, token);
    }

    internal override int EmitError(ErrorData error, OperationToken token)
    {
        Console.Write(error.ToConsoleString());
        return base.EmitError(error, token);
    }

    internal override void EmitEvent<T>(T ev, OperationToken token, bool clientOnly = false)
    {
        Console.Write(ev.ToConsoleString());
    }

    internal override void EmitAuthenticationRequest<T>(
        T authEvent,
        OperationToken token,
        AuthenticationManager.AuthAgentType authAgentType = AuthenticationManager.AuthAgentType.None
    )
    {
        if (Readline.TryReadInput(authEvent.ToConsoleString(), authEvent.TimeoutMs, out var input))
            authEvent.TryAccept(input.Trim());
    }
}

internal static class Readline
{
    private static readonly AutoResetEvent ReadHandle = new(false);
    private static readonly AutoResetEvent WaitHandle = new(false);
    private static string _input;

    static Readline()
    {
        _input = "";

        var inputThread = new Thread(Reader) { IsBackground = true };
        inputThread.Start();
    }

    internal static bool TryReadInput(string prompt, int timeout, out string input)
    {
        Console.Write(prompt + " ");
        input = "";

        ReadHandle.Set();
        if (WaitHandle.WaitOne(timeout))
        {
            input = _input;
            return true;
        }

        return false;
    }

    private static void Reader()
    {
        while (true)
        {
            ReadHandle.WaitOne();
            _input = Console.ReadLine() ?? "";
            WaitHandle.Set();
        }
    }
}
