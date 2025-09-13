using System.Collections.Concurrent;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Stack.Base;
using InTheHand.Net;

namespace Haraltd.Stack.Obex.Session;

public class ObexSessions<TClient, TClientProperties, TServer, TSubServer, TServerProperties>(
    IBluetoothStack stack
) : IObexSessions, IDisposable
    where TClient : ObexClient<TClientProperties>
    where TClientProperties : ObexClientProperties, new()
    where TServer : ObexServer<TSubServer, TServerProperties>
    where TSubServer : ObexSubServer<TServerProperties>, new()
    where TServerProperties : ObexServerProperties, new()
{
    private readonly ConcurrentDictionary<BluetoothAddress, IObexSession> _sessions = [];

    public async ValueTask<ErrorData> StartAsync(
        OperationToken token,
        BluetoothAddress address,
        IObexSession session
    )
    {
        return await StartAsyncByAddress(token, address, session);
    }

    public async ValueTask<ErrorData> StartAsyncByAddress(
        OperationToken token,
        BluetoothAddress addr,
        IObexSession session
    )
    {
        if (!_sessions.TryAdd(addr, session))
        {
            session.Dispose();
            return Errors.ErrorDeviceFileTransferSession.AddMetadata(
                "exception",
                "A session already exists with this address"
            );
        }

        session.Address = addr;
        session.Token = token;
        session.Stack = stack;
        session.Sessions = this;

        return await session.StartAsync();
    }

    public async ValueTask<ErrorData> StopAsync(OperationToken token, BluetoothAddress address)
    {
        if (!_sessions.TryGetValue(address, out var session))
            return Errors.ErrorDeviceFileTransferSession.AddMetadata(
                "exception",
                "No session exists for Object Push"
            );

        if (session is TClient or TServer && session.Token.ClientId != token.ClientId)
            return Errors.ErrorDeviceFileTransferSession.AddMetadata(
                "exception",
                "This transfer is controlled by another client"
            );

        return await session.StopAsync();
    }

    public bool GetClientSession(
        OperationToken token,
        BluetoothAddress address,
        out TClient session,
        out ErrorData error
    )
    {
        return GetSession(token, address, true, out session, out error);
    }

    public bool GetServerSession(
        OperationToken token,
        BluetoothAddress address,
        out TServer session,
        out ErrorData error
    )
    {
        return GetSession(token, address, true, out session, out error);
    }

    public bool GetServerTransferSession(
        OperationToken token,
        BluetoothAddress address,
        out TSubServer session,
        out ErrorData error
    )
    {
        return GetSession(token, address, false, out session, out error);
    }

    public void RemoveSession(IObexSession session)
    {
        _sessions.TryRemove(session.Address, out _);
    }

    public bool GetSession<TObexSession>(
        OperationToken token,
        BluetoothAddress address,
        bool checkClientId,
        out TObexSession session,
        out ErrorData error
    )
        where TObexSession : class, IObexSession
    {
        session = null;
        error = Errors.ErrorNone;

        if (!GetSession(token, address, checkClientId, out session))
            error = Errors.ErrorDeviceFileTransferSession.AddMetadata(
                "exception",
                "No session exists for Object Push Client"
            );

        return session != null;
    }

    public bool GetSession<TObexSession>(
        OperationToken token,
        BluetoothAddress addr,
        bool checkClientId,
        out TObexSession session
    )
        where TObexSession : class, IObexSession
    {
        session = null;

        foreach (var (_, s) in _sessions)
            if (s is TObexSession obexSession && obexSession.Address == addr)
            {
                if (checkClientId && obexSession.Token.ClientId != token.ClientId)
                    continue;

                session = obexSession;
                break;
            }

        return session != null;
    }

    public IEnumerable<T> EnumerateSessions<T>()
        where T : class, IObexSession
    {
        foreach (var (_, s) in _sessions)
            if (s is T session)
                yield return session;
    }

    public void Dispose()
    {
        foreach (var (_, s) in _sessions)
            s.StopAsync().AsTask().Wait();

        _sessions.Clear();

        GC.SuppressFinalize(this);
    }
}
