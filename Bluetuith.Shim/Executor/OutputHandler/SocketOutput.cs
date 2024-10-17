using Bluetuith.Shim.Types;
using DotMake.CommandLine;
using NetCoreServer;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.CommandLine;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bluetuith.Shim.Executor.OutputHandler;

internal sealed class SocketOutput : OutputBase
{
    private readonly string _socketPath;
    private readonly SocketServer _socketServer;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private OperationToken _operationToken;

    private readonly ConcurrentDictionary<int, AuthenticationEvent> authenticationEvents = new();
    private JsonObject output = new()
    {
        ["operationId"] = 0,
        ["requestId"] = 0,
        ["status"] = "",
    };
    private JsonObject outputEvent = new()
    {
        ["operationId"] = 0,
        ["requestId"] = 0,
        ["eventId"] = 0
    };

    public SocketOutput(string socketPath, OperationToken token)
    {
        if (_socketServer != null && _socketServer.IsStarted)
        {
            throw new ArgumentException("The socket server has already started");
        }

        if (File.Exists(socketPath))
        {
            File.Delete(socketPath);
        }

        _socketServer = new SocketServer(socketPath)
        {
            OptionSendBufferSize = ushort.MaxValue
        };
        _socketServer.Start();

        _socketPath = socketPath;
        _operationToken = token;
    }

    public override int EmitResult<T>(T result, OperationToken token)
    {
        try
        {
            _semaphore.Wait();

            (var operationId, var requestId) = GetIdsFromToken(token);

            output["operationId"] = operationId;
            output["requestId"] = requestId;
            output["status"] = "ok";
            output["data"] = JsonSerializer.SerializeToNode(result.ToJsonObject());

            SendReply(ref output);

            output["operationId"] = 0;
            output["requestId"] = 0;
            output["status"] = "";
            output.Remove("data");
        }
        finally { _semaphore.Release(); }

        return base.EmitResult(result, token);
    }

    public override int EmitError(ErrorData error, OperationToken token)
    {
        if (error == Errors.ErrorNone)
        {
            return EmitResult(error, token);
        }

        try
        {
            _semaphore.Wait();

            (var operationId, var requestId) = GetIdsFromToken(token);

            output["operationId"] = operationId;
            output["requestId"] = requestId;
            output["status"] = "error";
            output["error"] = JsonSerializer.SerializeToNode(error.ToJsonObject());

            SendReply(ref output);

            output["operationId"] = 0;
            output["requestId"] = 0;
            output["status"] = "";
            output.Remove("error");
        }
        finally { _semaphore.Release(); }

        return base.EmitError(error, token);
    }

    public override void EmitEvent<T>(T ev, OperationToken token)
    {
        try
        {
            _semaphore.Wait();

            (var operationId, var requestId) = GetIdsFromToken(token);

            outputEvent["operationId"] = operationId;
            outputEvent["requestId"] = requestId;
            outputEvent["eventId"] = ev.EventType.Value;
            outputEvent["event"] = JsonSerializer.SerializeToNode(ev.ToJsonObject());

            SendReply(ref outputEvent);

            outputEvent["operationId"] = 0;
            outputEvent["requestId"] = 0;
            outputEvent["eventId"] = "";
            outputEvent.Remove("event");
        }
        finally { _semaphore.Release(); }
    }

    public override void EmitAuthenticationRequest<T>(T authEvent, OperationToken token)
    {
        EmitEvent(authEvent, token);

        if (authenticationEvents.TryAdd(token.OperationId, authEvent))
        {
            token.CancelToken.WaitHandle.WaitOne();
            authenticationEvents.TryRemove(token.OperationId, out _);
        }
        else
        {
            authEvent.SetResponse();
        }
    }

    public override void SetAuthenticationResponse(int operationId, string response)
    {
        if (authenticationEvents.TryRemove(operationId, out AuthenticationEvent? authEvent))
        {
            authEvent.SetResponse(response);
        }
    }

    public override void WaitForClose()
    {
        Console.WriteLine($"[+] Server started at '{Path.GetFileName(_socketPath)}' ({_socketServer.Id})");

        _operationToken.CancelToken.WaitHandle.WaitOne();
        _socketServer.Stop();

        Console.WriteLine("[+] Server stopped");
    }

    private void SendReply(ref JsonObject reply)
    {
        try
        {
            _socketServer.Multicast(PackMessage(ref reply));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Socket error: Could not send reply: {ex.Message}");
        }
    }

    private ReadOnlySpan<byte> PackMessage(ref JsonObject reply)
    {
        ReadOnlySpan<byte> serializedReply = JsonSerializer.SerializeToUtf8Bytes(reply);

        Span<byte> replyHeader = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(replyHeader, serializedReply.Length);

        var packedMessage = new byte[replyHeader.Length + serializedReply.Length];
        replyHeader.CopyTo(packedMessage);
        serializedReply.CopyTo(packedMessage.AsSpan(replyHeader.Length));

        return new ReadOnlySpan<byte>(packedMessage);
    }

    private (int, int) GetIdsFromToken(OperationToken token)
    {
        return token == OperationToken.Empty ? (0, 0) : (token.OperationId, token.RequestId);
    }
}

internal class SocketSession : UdsSession
{
    private record struct Request
    {
        public List<string> command { get; set; }
        public int requestId { get; set; }
    }

    private record class Reply : Result
    {
        public string command { get; }

        public Reply(string _command)
        {
            command = _command;
        }

        public override JsonObject ToJsonObject()
        {
            return new JsonObject()
            {
                ["command"] = command,
            };
        }
    }

    public SocketSession(UdsServer server) : base(server) { }

    protected override void OnReceived(byte[] buffer, long offset, long size)
    {
        OperationToken token = OperationToken.Empty;

        try
        {
            Request request = JsonSerializer.Deserialize<Request>(new ArraySegment<byte>(buffer, (int)offset, (int)size));
            if (request.requestId <= 0)
            {
                throw new ArgumentException("The request ID must be a non-zero positive integer");
            }

            token = Operation.GenerateToken(request.requestId);
            request.command.InsertRange(0, [
                "--operation-id", token.OperationId.ToString(),
                "--request-id", token.RequestId.ToString()
            ]);

            ParseResult parsed = Cli.Parse<Commands>(
                request.command.ToArray(),
                new CliSettings() { Output = TextWriter.Null }
            );
            if (parsed.Errors.Count > 0)
            {
                throw new ArgumentException($"Command parse error: {parsed.Errors[0].Message}");
            }

            Output.Result(new Reply(parsed.CommandResult.Command.Name), Errors.ErrorNone, token);

            Task.Run(async () => await Operation.ExecuteAsync(token, parsed));
        }
        catch (Exception ex)
        {
            ErrorData error = SocketErrors.ErrorJsonRequestParse.WrapError(new()
            {
                {"exception", ex.Message }
            });

            Output.Error(error, token);
        }
    }

    protected override void OnError(SocketError error)
    {
        Console.WriteLine($"Socket error: Could not send reply: {error}");
    }
}

internal class SocketServer : UdsServer
{
    public SocketServer(string path) : base(path) { }

    protected override UdsSession CreateSession() { return new SocketSession(this); }

    protected override void OnError(SocketError error)
    {
        Console.WriteLine($"Socket error: {error}");
    }
}

internal class SocketErrors : Errors
{
    public static ErrorData ErrorJsonRequestParse = new(
        Code: new ErrorCode("ERROR_JSON_REQUEST_PARSE", -2500),
        Description: "An error occurred while parsing the JSON request",
        Metadata: new() {
            {"exception", ""}
        }
    );
}

