using DotNext.Threading.Tasks;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.Managers;
using Haraltd.Stack.Base;
using Haraltd.Stack.Base.Sockets;
using Haraltd.Stack.Obex.Packet;
using InTheHand.Net;

namespace Haraltd.Stack.Obex.Session;

public record ObexServerProperties : ObexSessionProperties;

public class ObexServer<TSubServer, TProperties>(ObexService service, TProperties properties)
    : IObexSession
    where TSubServer : ObexSubServer<TProperties>, new()
    where TProperties : ObexServerProperties, new()
{
    private ISocketListener _listener;

    public BluetoothAddress Address { get; set; }
    public OperationToken Token { get; set; }
    public IBluetoothStack Stack { get; set; }
    public IObexSessions Sessions { get; set; }

    public async ValueTask<ErrorData> StartAsync()
    {
        (_listener, var error) = Stack.CreateSocketListener(
            service.GetServiceSocketListenerOptions()
        );
        try
        {
            error = _listener != null ? await _listener.StartAdvertisingAsync() : error;
        }
        finally
        {
            if (_listener != null && error == Errors.ErrorNone)
            {
                _listener.OnConnected += OnConnected;
                OperationManager.SetOperationProperties(Token);
            }
        }

        return error;
    }

    public virtual ValueTask<ErrorData> StopAsync()
    {
        Dispose();
        return ValueTask.FromResult(Errors.ErrorNone);
    }

    public virtual async ValueTask<ErrorData> CancelTransferAsync()
    {
        return await ValueTask.FromResult(Errors.ErrorNone);
    }

    private void OnConnected(ISocket socket)
    {
        try
        {
            _ = Task.Run(async () =>
            {
                var subServer = new TSubServer();
                subServer.Initialize(socket, Token.LinkedCancelTokenSource, properties);

                try
                {
                    await Sessions.StartAsyncByAddress(Token, socket.Address, subServer);
                }
                catch
                {
                    subServer.Dispose();
                }
            });
        }
        catch
        {
            // ignored
        }
    }

    public void Dispose()
    {
        try
        {
            _listener.OnConnected -= OnConnected;
            _listener.Dispose();

            Token.Release();

            foreach (var session in Sessions.EnumerateSessions<TSubServer>())
                session.StopAsync().Wait();
        }
        catch
        {
            // ignored
        }
        finally
        {
            Sessions.RemoveSession(this);

            GC.SuppressFinalize(this);
        }
    }
}

public class ObexSubServer<T> : IObexSession
    where T : ObexServerProperties, new()
{
    protected T Properties;
    protected ISocket Socket;
    protected CancellationTokenSource Cts;

    public BluetoothAddress Address { get; set; }
    public OperationToken Token { get; set; }
    public IBluetoothStack Stack { get; set; }
    public IObexSessions Sessions { get; set; }

    public virtual async ValueTask<ErrorData> StartAsync()
    {
        await ValueTask.CompletedTask;

        var canSubscribe = Socket.CanSubscribeToEvents;
        if (canSubscribe)
        {
            Socket.ConnectionStatusEvent += OnConnectionStateChanged;
        }

        return canSubscribe ? Errors.ErrorNone : Errors.ErrorDeviceNotFound;
    }

    public virtual ValueTask<ErrorData> StopAsync()
    {
        Dispose();
        return ValueTask.FromResult(Errors.ErrorNone);
    }

    public virtual async ValueTask<ErrorData> CancelTransferAsync()
    {
        await Cts.CancelAsync();

        return Errors.ErrorNone;
    }

    private void OnConnectionStateChanged(bool connected)
    {
        if (!connected)
            Dispose();
    }

    public void Initialize(ISocket socket, CancellationTokenSource ctx, T properties)
    {
        Socket = socket;
        Cts = ctx;
        Properties = properties;
    }

    public virtual void Dispose()
    {
        Socket.ConnectionStatusEvent -= OnConnectionStateChanged;

        Cts.Dispose();
        Socket.Dispose();

        Sessions.RemoveSession(this);

        GC.SuppressFinalize(this);
    }
}
