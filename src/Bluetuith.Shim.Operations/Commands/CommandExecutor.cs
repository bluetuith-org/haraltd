using Bluetuith.Shim.DataTypes.Generic;
using Bluetuith.Shim.Operations.Managers;

namespace Bluetuith.Shim.Operations.Commands;

public static class CommandExecutor
{
    public static ErrorData Run(string[] args)
    {
        var token = OperationManager.GenerateToken(0, Guid.NewGuid());

        OperationManager.ExecuteHandler(token, args);
        OperationManager.WaitForExtendedOperations();

        return Errors.ErrorNone;
    }

    public static Task StopAsync()
    {
        return OperationManager.CancelAllAsync();
    }
}

public static class ExecutorErrors
{
    public static readonly ErrorData ErrorParsingCommand = new(
        new ErrorCode("ERROR_PARSING_COMMAND", -10000),
        "An error occurred while parsing the command-line arguments"
    );
}