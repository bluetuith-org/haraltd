using Bluetuith.Shim.Types;

namespace Bluetuith.Shim.Executor.OutputHandler;

public static class Output
{
    private static OutputBase _output = new CommandOutput();

    public static ErrorData StartSocketServer(string socketPath, OperationToken token)
    {
        try
        {
            _output = new SocketOutput(socketPath, token);
            _output.WaitForClose();
        }
        catch (Exception e)
        {
            return Errors.ErrorOperationCancelled.WrapError(new()
            {
                {"exception", e.Message}
            });
        }

        return Errors.ErrorNone;
    }

    public static int Result<T>(T result, ErrorData error, OperationToken token) where T : Result
    {
        if (error != Errors.ErrorNone)
        {
            return Error(error, token);
        }

        Result outputResult = result == null ? GenericResult<string>.Empty() : result;
        return _output.EmitResult(outputResult, token);
    }

    public static int Error(ErrorData error, OperationToken token)
    {
        return _output.EmitError(error, token);
    }

    public static void Event<T>(
        T ev,
        OperationToken token
    ) where T : Event
    {
        _output.EmitEvent(ev, token);
    }

    public static bool ConfirmAuthentication<T>(T authEvent, OperationToken token) where T : AuthenticationEvent
    {
        Task.Run(() => _output.EmitAuthenticationRequest(authEvent, token));
        return authEvent.WaitForResponse();
    }

    public static void ReplyToAuthenticationRequest(int operationId, string response)
    {
        Task.Run(() => _output.SetAuthenticationResponse(operationId, response));
    }
}

public abstract class OutputBase
{
    public virtual int EmitResult<T>(T result, OperationToken token) where T : Result
    {
        return Errors.ErrorNone.Code.Value;
    }

    public virtual int EmitError(ErrorData error, OperationToken token)
    {
        return error.Code.Value;
    }

    public virtual void EmitEvent<T>(T ev, OperationToken token) where T : Event
    {
    }

    public virtual void EmitAuthenticationRequest<T>(T authEvent, OperationToken token) where T : AuthenticationEvent
    {
    }

    public virtual void SetAuthenticationResponse(int operationId, string response)
    {
    }

    public virtual void WaitForClose()
    {

    }
}
