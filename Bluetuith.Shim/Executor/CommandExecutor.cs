using Bluetuith.Shim.Types;
using DotMake.CommandLine;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace Bluetuith.Shim.Executor;

public static class CommandExecutor
{
    public static async Task<ErrorData> RunAsync(string[] args)
    {
        ParseResult parsed = Cli.Parse<Commands>(args);
        if (parsed.Errors.Count > 0)
        {
            // Work-around for an exception which is thrown when "-v|--version" is parsed
            // only using Cli.Parse().
            // Exception message: -v option cannot be combined with other arguments.
            foreach (CliToken parsedToken in parsed.Tokens)
            {
                if (parsedToken.Type == CliTokenType.Option &&
                    (parsedToken.Value.Equals("-v") || parsedToken.Value.Equals("--version")))
                {
                    goto execute;
                }
            }

            return ExecutorErrors.ErrorParsingCommand.WrapError(new()
            {
                {"exception", parsed.Errors[0].Message}
            });
        }

    execute:
        OperationToken token = Operation.GenerateToken(0);
        await Operation.ExecuteAsync(token, parsed);

        return Errors.ErrorNone;
    }

    public static Task StopAsync()
    {
        return Operation.CancelAllAsync();
    }
}

internal class ExecutorErrors : Errors
{
    public static ErrorData ErrorParsingCommand = new(
        Code: new("ERROR_PARSING_COMMAND", -10000),
        Description: "An error occurred while parsing the command-line arguments",
        Metadata: new()
        {
            {"exception",  ""}
        }
    );
}