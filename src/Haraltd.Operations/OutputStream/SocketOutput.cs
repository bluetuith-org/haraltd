using System.Buffers;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using DotNext.Collections.Generic;
using DotNext.IO;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.Managers;
using NetCoreServer;
using static Haraltd.Operations.Managers.AuthenticationManager;

namespace Haraltd.Operations.OutputStream;

internal sealed class SocketOutput : OutputBase
{
    private readonly OperationToken _operationToken;
    private readonly string _socketPath;
    private readonly SocketServer _socketServer;

    internal SocketOutput(string socketPath, OperationToken token)
    {
        if (_socketServer is { IsStarted: true })
            throw new ArgumentException("The socket server has already started");

        var socketDir =
            Path.GetDirectoryName(socketPath)
            ?? throw new InvalidDataException("Cannot find socket directory");

        if (!Path.Exists(socketDir))
            Directory.CreateDirectory(socketDir);

        if (File.Exists(socketPath))
            File.Delete(socketPath);

        _socketServer = new SocketServer(socketPath);
        _socketServer.Start();

        _socketPath = socketPath;
        _operationToken = token;
    }

    internal override int EmitResult<T>(T result, OperationToken token)
    {
        return !SendMarshalledResult(result, token)
            ? SocketErrors.ErrorJsonResponseMarshal.Code.Value
            : base.EmitResult(result, token);
    }

    internal override int EmitError(ErrorData error, OperationToken token)
    {
        return !SendMarshalledError(error, token)
            ? SocketErrors.ErrorJsonResponseMarshal.Code.Value
            : base.EmitError(error, token);
    }

    internal override void EmitEvent<T>(T ev, OperationToken token, bool clientOnly = false)
    {
        SendMarshalledEvent(ev, token, clientOnly);
    }

    internal override void EmitAuthenticationRequest<T>(
        T authEvent,
        OperationToken token,
        AuthAgentType authAgentType = AuthAgentType.None
    )
    {
        if (AddEvent(token, authEvent, authAgentType))
            if (!SendMarshalledEvent(authEvent, token, true))
                SetAuthenticationResponse(token, authEvent.CurrentAuthId, "");
    }

#nullable enable
    internal override bool SetAuthenticationResponse(
        OperationToken token,
        long authId,
        string response
    )
    {
        return SetEventResponse(token, authId, response);
    }

#nullable disable

    internal override void WaitForClose()
    {
        Console.WriteLine(
            $"[+] Server started at '{Path.GetFileName(_socketPath)}' ({_socketServer.Id})"
        );

        _operationToken.Wait();
        _socketServer.Stop();

        Console.WriteLine("[+] Server stopped");
    }

    private bool SendMarshalledResult<T>(T reply, OperationToken token, bool hasData = true)
        where T : IResult
    {
        return _socketServer.SendReply(
            token,
            true,
            new ResultMarshaller<IResult>(reply, hasData, false)
        );
    }

    private bool SendMarshalledError(ErrorData error, OperationToken token)
    {
        if (error == Errors.ErrorNone)
            return SendMarshalledResult(error, token, false);

        return _socketServer.SendReply(
            token,
            true,
            new ResultMarshaller<IResult>(error, true, true)
        );
    }

    private bool SendMarshalledEvent<T>(T ev, OperationToken token, bool clientOnly)
        where T : IEvent
    {
        return _socketServer.SendReply(token, clientOnly, new EventMarshaller<IEvent>(ev));
    }
}

internal class SocketSession(SocketServer server) : UdsSession(server)
{
    protected override void OnReceived(byte[] buffer, long offset, long size)
    {
        try
        {
            foreach (
                var request in JsonSerializer
                    .DeserializeAsyncEnumerable(
                        new ReadOnlySequence<byte>(buffer[(int)offset..(int)size]).AsStream(),
                        RequestSerializable.Default.Request,
                        true
                    )
                    .ToBlockingEnumerable()
            )
            {
                var token = OperationManager.GenerateToken(request.RequestId, Id);

                Task.Factory.StartNew(
                    Execute,
                    new ParseRequest { ClientRequest = request, Token = token }
                );
            }
        }
        catch (Exception ex)
        {
            Output.Event(
                SocketErrors.ErrorJsonRequestParse.AddMetadata("exception", ex.Message),
                OperationToken.None
            );
        }
    }

    private static void Execute(object obj)
    {
        var result = obj as ParseRequest;
        OperationManager.ExecuteHandler(result!.Token, result.ClientRequest.Command);
    }

    protected override void OnDisconnected()
    {
        OperationManager.CancelClientOperations(Id);
        RemoveAgentsAndEvents(Id);
    }

    protected override void OnError(SocketError error)
    {
        Console.WriteLine($"Socket error: Could not send reply: {error}");
    }

    private record ParseRequest
    {
        public Request ClientRequest;
        public OperationToken Token;
    }
}

internal class SocketServer : UdsServer
{
    private static readonly ConcurrentDictionary<Guid, SessionStream> SessionStreams = new();

    private static readonly Func<IEnumerable<SessionStream>> Values = (
        SessionStreams as IDictionary<Guid, SessionStream>
    ).ValuesGetter();

    internal SocketServer(string path)
        : base(path) { }

    internal bool SendReply<T>(OperationToken token, bool clientOnlyData, T marshaller)
        where T : IMarshaller
    {
        if (!IsStarted)
            return false;

        var unsent = 0;

        try
        {
            var foundClient = false;

            foreach (var session in Values())
            {
                if (session == null || session.IsDisposed)
                    continue;

                if (clientOnlyData)
                {
                    if (session.Id == token.ClientId)
                        foundClient = true;
                    else
                        continue;
                }

                if (!session.Enqueue(marshaller, token))
                    unsent++;

                if (foundClient)
                    break;
            }

            return unsent == 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Socket error: Could not send reply: {ex.Message}");
            return false;
        }
    }

    protected override UdsSession CreateSession()
    {
        return new SocketSession(this);
    }

    protected override void OnConnecting(UdsSession session)
    {
        SessionStreams.TryAdd(session.Id, new SessionStream(session));
    }

    protected override void OnDisconnecting(UdsSession session)
    {
        if (SessionStreams.TryRemove(session.Id, out var sessionStream))
            sessionStream.Dispose();
    }

    protected override void OnError(SocketError error)
    {
        Console.WriteLine($"Socket error: {error}");
    }

    private class SessionStream
    {
        private record SessionReply<T>
            where T : IMarshaller
        {
            public T Marshaller = default!;
            public OperationToken Token;
        }

        private static readonly byte[] NewLine = "\r\n"u8.ToArray();
        private static readonly JsonWriterOptions Options = new() { SkipValidation = true };
        private UdsSession _session;
        private NetworkStream _stream;
        private readonly Channel<SessionReply<IMarshaller>> _replyQueue = Channel.CreateUnbounded<
            SessionReply<IMarshaller>
        >(
            new UnboundedChannelOptions
            {
                AllowSynchronousContinuations = true,
                SingleReader = true,
                SingleWriter = false,
            }
        );

        private Utf8JsonWriter Writer { get; set; }

        public bool IsDisposed => _session.IsDisposed || _session.IsSocketDisposed;

        public Guid Id => _session.Id;

        public SessionStream(UdsSession session)
        {
            Initialize(session);
        }

        private void Initialize(UdsSession session)
        {
            _session = session;
            _stream = new NetworkStream(_session.Socket, FileAccess.Write, false);
            Writer = new Utf8JsonWriter(_stream, Options);
            Task.Run(async () =>
            {
                while (
                    await _replyQueue.Reader.WaitToReadAsync()
                    && _replyQueue.Reader.TryRead(out var reply)
                )
                {
                    try
                    {
                        reply.Marshaller.Write(reply.Token, Writer);
                        await Flush();
                    }
                    catch (Exception ex)
                    {
                        if (ex is NullReferenceException || !Reset())
                            Console.WriteLine(
                                $"An exception occurred while writing reply: {ex.Message}"
                            );
                    }
                }
            });
        }

        public bool Enqueue<T>(T marshaller, OperationToken token)
            where T : IMarshaller
        {
            return _replyQueue.Writer.TryWrite(
                new SessionReply<IMarshaller>() { Marshaller = marshaller, Token = token }
            );
        }

        private bool Reset()
        {
            if (_session == null || IsDisposed)
                return false;

            try
            {
                Writer?.Dispose();
                _stream?.Dispose();

                Initialize(_session);
            }
            catch
            {
                SessionStreams.TryRemove(Id, out _);
                return false;
            }

            return true;
        }

        private async Task Flush()
        {
            await Writer.FlushAsync();
            _stream.Write(NewLine);

            Writer.Reset();
            await Task.Delay(10);
        }

        public void Dispose()
        {
            try
            {
                _replyQueue.Writer.TryComplete();

                Writer.Dispose();
                _stream.Dispose();
            }
            catch { }
        }
    }
}

internal record struct Request
{
    [JsonPropertyName("command")]
    public string[] Command { get; set; }

    [JsonPropertyName("request_id")]
    public long RequestId { get; set; }
}

[JsonSerializable(typeof(Request))]
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    DefaultBufferSize = 8192
)]
internal partial class RequestSerializable : JsonSerializerContext;

internal abstract class SocketErrors : Errors
{
    internal static readonly ErrorData ErrorJsonRequestParse = new(
        new ErrorCode("ERROR_JSON_REQUEST_PARSE", -2500),
        "An error occurred while parsing the JSON request"
    );

    internal static readonly ErrorData ErrorJsonResponseMarshal = new(
        new ErrorCode("ERROR_JSON_RESPONSE_MARSHAL", -2511),
        "An error occurred while marshalling the JSON request"
    );
}
