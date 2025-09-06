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
    private static readonly UserDataSlot<OperationToken> Slot = new();

    static CommandParser()
    {
        App.UseFilter<ParserFilter>();

        if (Output.IsOnSocket)
        {
            ConsoleApp.Log = static delegate { };
            ConsoleApp.LogError = static msg =>
                Output.Event(
                    Errors.ErrorUnexpected.AddMetadata("command_error", msg),
                    OperationToken.None
                );
        }
    }

    public static void Parse(OperationToken token, string[] args)
    {
        args.GetUserData().Set(Slot, token);

        App.Run(args);
    }

    internal sealed class ParserFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
    {
        public override async Task InvokeAsync(
            ConsoleAppContext context,
            CancellationToken cancellationToken
        )
        {
            var token = OperationToken.None;
            var exitCode = 0;

            try
            {
                if (!context.Arguments.GetUserData().TryGet(Slot, out token))
                    throw new InvalidDataException(
                        "Token was not found from the attached data slot"
                    );

                context = context with { State = token };

                await Next.InvokeAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                exitCode = 1;

                Output.Error(
                    ExecutorErrors.ErrorParsingCommand.AddMetadata("exception", ex.Message),
                    token
                );
            }
            finally
            {
                OperationManager.RemoveToken(token);

                if (!Output.IsOnSocket)
                    Environment.ExitCode = exitCode;
            }
        }
    }
}
