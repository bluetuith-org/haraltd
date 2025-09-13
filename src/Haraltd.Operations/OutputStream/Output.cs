using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.Commands;
using static Haraltd.Operations.Managers.AuthenticationManager;

namespace Haraltd.Operations.OutputStream;

public static class Output
{
    private static OutputBase _output = new CommandOutput();
    private static bool _waitOnOutput = true;

    public static bool IsOnSocket { get; private set; }

    public static ErrorData StartSocketServer(string socketPath, OperationToken token)
    {
        try
        {
            _output = new SocketOutput(socketPath, token);

            IsOnSocket = true;
            _waitOnOutput = false;

            Console.WriteLine($"[+] Server started at '{socketPath}'");
        }
        catch (Exception e)
        {
            return Errors.ErrorOperationCancelled.WrapError(
                new Dictionary<string, object> { { "exception", e.Message } }
            );
        }

        return Errors.ErrorNone;
    }

    public static void SetContinue()
    {
        _waitOnOutput = false;
    }

    public static void Close()
    {
        _output.Close();
    }

    public static int Result<T>(T result, ErrorData error, OperationToken token)
        where T : IResult
    {
        if (error != Errors.ErrorNone)
            return Error(error, token);

        return _output.EmitResult(result, token);
    }

    public static int ResultWithContext<T>(
        T result,
        ErrorData error,
        OperationToken token,
        CommandParserContext context
    )
        where T : IResult
    {
        if (error != Errors.ErrorNone)
            return ErrorWithContext(error, token, context);

        if (context != null)
        {
            context.Error = error;
        }

        return _output.EmitResult(result, token);
    }

    public static int Error(ErrorData error, OperationToken token)
    {
        return _output.EmitError(error, token);
    }

    public static int ErrorWithContext(
        ErrorData error,
        OperationToken token,
        CommandParserContext context
    )
    {
        if (context != null)
        {
            context.Error = error;
            if (!context.ShouldOutputError)
                return 1;
        }

        return _output.EmitError(error, token);
    }

    public static void Event<T>(T ev, OperationToken token)
        where T : IEvent
    {
        if (_waitOnOutput)
            return;

        _output.EmitEvent(ev, token);
    }

    public static void ClientEvent<T>(T ev, OperationToken token)
        where T : IEvent
    {
        if (_waitOnOutput)
            return;

        _output.EmitEvent(ev, token, true);
    }

    public static bool ConfirmAuthentication<T>(
        T authEvent,
        AuthAgentType authAgentType = AuthAgentType.None
    )
        where T : AuthenticationEvent
    {
        if (_waitOnOutput)
            return false;

        _ = Task.Run(() =>
            _output.EmitAuthenticationRequest(authEvent, authEvent.Token, authAgentType)
        );
        return authEvent.WaitForResponse();
    }

    public static bool ReplyToAuthenticationRequest(
        OperationToken token,
        long authId,
        string response
    )
    {
        if (_waitOnOutput)
            return false;

        return _output.SetAuthenticationResponse(token, authId, response);
    }
}

internal abstract class OutputBase
{
    internal virtual int EmitResult<T>(T result, OperationToken token)
        where T : IResult
    {
        return Errors.ErrorNone.Code.Value;
    }

    internal virtual int EmitError(ErrorData error, OperationToken token)
    {
        return error.Code.Value;
    }

    internal virtual void EmitEvent<T>(T ev, OperationToken token, bool clientOnly = false)
        where T : IEvent { }

    internal virtual void EmitAuthenticationRequest<T>(
        T authEvent,
        OperationToken token,
        AuthAgentType authAgentType = AuthAgentType.None
    )
        where T : AuthenticationEvent { }

    internal virtual bool SetAuthenticationResponse(
        OperationToken token,
        long authId,
        string response
    )
    {
        return true;
    }

    internal virtual void Close() { }
}
