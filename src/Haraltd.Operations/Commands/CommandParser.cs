using ConsoleAppFramework;
using DotNext;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.Managers;
using Haraltd.Operations.OutputStream;

namespace Haraltd.Operations.Commands;

public static class CommandParser
{
    private static readonly ConsoleApp.ConsoleAppBuilder App = ConsoleApp.Create();
    private static readonly UserDataSlot<CommandParserContext> Slot = new();

    static CommandParser()
    {
        App.UseFilter<ServerCommandFilter>();
        App.UseFilter<ParserFilter>();

        ConsoleApp.Log = msg =>
        {
            Console.WriteLine(msg);
            Environment.Exit(0);
        };

        ConsoleApp.LogError = msg =>
        {
            Console.WriteLine(msg);
            Environment.Exit(1);
        };
    }

    public static void Parse(
        OperationToken token,
        string[] args,
        CommandParserContext parserContext = null
    )
    {
        parserContext ??= new CommandParserContext(token, true);

        args.GetUserData().Set(Slot, parserContext);

        App.Run(args);
    }

    // TODO: Please change this.
    internal sealed class ServerCommandFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
    {
        public override async Task InvokeAsync(
            ConsoleAppContext context,
            CancellationToken cancellationToken
        )
        {
            if (!context.Arguments.GetUserData().TryGet(Slot, out var parserInfo))
                return;

            var isServerCommand =
                context.Arguments.First() is "server"
                && context.Arguments.Length > 1
                && context.Arguments[1] is "start";

            switch (isServerCommand)
            {
                case true:
                    ConsoleApp.Log = static delegate { };
                    ConsoleApp.LogError = static delegate { };
                    break;

                case false:
                    Output.SetContinue();
                    break;
            }

            if (parserInfo != null)
                parserInfo.IsServerCommand = isServerCommand;

            await Next.InvokeAsync(context, cancellationToken);
        }
    }

    internal sealed class ParserFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
    {
        public override async Task InvokeAsync(
            ConsoleAppContext context,
            CancellationToken cancellationToken
        )
        {
            var error = Errors.ErrorNone;

            if (!context.Arguments.GetUserData().TryGet(Slot, out var parserInfo))
                return;

            context = context with { State = parserInfo };

            try
            {
                await Next.InvokeAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                error = ExecutorErrors.ErrorParsingCommand.AddMetadata("exception", ex.Message);
                if (parserInfo.ShouldOutputError)
                    Output.Error(error, parserInfo.Token);
            }
            finally
            {
                OperationManager.RemoveToken(parserInfo.Token);
                parserInfo.Error = error;
                parserInfo.IsParsed = false;

                context.Arguments.GetUserData().Remove(Slot);
            }
        }
    }
}

public record class CommandParserContext
{
    private readonly ManualResetEvent _commandContinuationEvent;

    private readonly TaskCompletionSource<bool> _isServerCommandCheck;
    private readonly TaskCompletionSource<bool> _isCommandParsed;
    private readonly TaskCompletionSource<ErrorData> _commandCompletedWithError;

    internal readonly bool ShouldOutputError = false;
    internal OperationToken Token { get; private set; }

    public CommandParserContext(OperationToken token)
    {
        Token = token;
        _commandContinuationEvent = new ManualResetEvent(false);
        _isCommandParsed = new TaskCompletionSource<bool>();
        _isServerCommandCheck = new TaskCompletionSource<bool>();
        _commandCompletedWithError = new TaskCompletionSource<ErrorData>();
    }

    internal CommandParserContext(OperationToken token, bool shouldOutputError)
    {
        Token = token;
        ShouldOutputError = shouldOutputError;
    }

    public bool IsParsed
    {
        get
        {
            if (_isCommandParsed == null)
                throw new NullReferenceException($"{nameof(IsParsed)}");

            return _isCommandParsed.Task.Result;
        }
        set
        {
            if (_isCommandParsed == null)
                return;

            if (_isCommandParsed.Task.IsCompleted)
                return;

            _isCommandParsed.TrySetResult(value);
        }
    }

    public bool IsServerCommand
    {
        get
        {
            if (_isServerCommandCheck == null)
                throw new NullReferenceException($"{nameof(IsServerCommand)}");

            return _isServerCommandCheck.Task.Result;
        }
        set
        {
            if (_isServerCommandCheck == null)
                return;

            if (_isServerCommandCheck.Task.IsCompleted)
                return;

            _isServerCommandCheck.SetResult(value);
        }
    }

    public ErrorData Error
    {
        get
        {
            if (_commandCompletedWithError == null)
                throw new NullReferenceException($"{nameof(ErrorData)}");

            return _commandCompletedWithError.Task.Result;
        }
        set
        {
            if (_commandCompletedWithError == null)
                return;

            if (_commandCompletedWithError.Task.IsCompleted)
                return;

            _commandCompletedWithError.SetResult(value);
        }
    }

    public void ContinueCommandExecution() => _commandContinuationEvent?.Set();

    private void WaitForCommandContinuation() => _commandContinuationEvent?.WaitOne();

    internal void SetParsedAndWait()
    {
        if (_isCommandParsed == null)
            return;

        IsParsed = true;
        WaitForCommandContinuation();
    }
}
