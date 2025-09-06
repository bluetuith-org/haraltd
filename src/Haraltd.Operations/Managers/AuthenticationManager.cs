using DotNext.Threading;
using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;

namespace Haraltd.Operations.Managers;

public static class AuthenticationManager
{
    public enum AuthAgentType
    {
        None = 0,
        Pairing,
        Obex,
    }

    private static readonly Dictionary<Guid, Dictionary<long, AuthenticationEvent>> Events = new();

    private static Atomic<Guid> _pairingAgent;
    private static Atomic<Guid> _obexAgent;

    static AuthenticationManager()
    {
        _pairingAgent.Write(Guid.Empty);
        _obexAgent.Write(Guid.Empty);
    }

    internal static ErrorData RegisterAuthAgent(AuthAgentType authAgentType, Guid clientId)
    {
        if (clientId == Guid.Empty)
            return Errors.ErrorUnexpected.AddMetadata("exception", "Invalid client ID");

        var registered = false;

        switch (authAgentType)
        {
            case AuthAgentType.Pairing:
                registered = _pairingAgent.CompareAndSet(Guid.Empty, clientId);
                break;
            case AuthAgentType.Obex:
                registered = _obexAgent.CompareAndSet(Guid.Empty, clientId);
                break;
            case AuthAgentType.None:
                return Errors.ErrorUnexpected.AddMetadata(
                    "exception",
                    "No authentication agent specified"
                );
        }

        if (!registered)
            return Errors.ErrorUnexpected.AddMetadata(
                "exception",
                "Authentication agent is already registered"
            );

        return Errors.ErrorNone;
    }

    internal static ErrorData UnregisterAuthAgent(AuthAgentType authAgentType, Guid clientId)
    {
        if (clientId == Guid.Empty)
            return Errors.ErrorUnexpected.AddMetadata("exception", "Invalid client ID");

        switch (authAgentType)
        {
            case AuthAgentType.Pairing:
                _pairingAgent.CompareAndSet(clientId, Guid.Empty);
                break;
            case AuthAgentType.Obex:
                _obexAgent.CompareAndSet(clientId, Guid.Empty);
                break;
            case AuthAgentType.None:
                return Errors.ErrorUnexpected.AddMetadata(
                    "exception",
                    "No authentication agent specified"
                );
        }

        return Errors.ErrorNone;
    }

    public static bool IsAgent(Guid clientId, AuthAgentType authAgentType)
    {
        if (clientId == Guid.Empty)
            return false;

        var isAgent = false;
        switch (authAgentType)
        {
            case AuthAgentType.Pairing:
                _pairingAgent.Read(out var pairingAgent);
                isAgent = pairingAgent == clientId;
                break;
            case AuthAgentType.Obex:
                _obexAgent.Read(out var obexAgent);
                isAgent = obexAgent == clientId;
                break;
            case AuthAgentType.None:
            default:
                return false;
        }

        return isAgent;
    }

    internal static void RemoveAgentsAndEvents(Guid clientId)
    {
        UnregisterAuthAgent(AuthAgentType.Pairing, clientId);
        UnregisterAuthAgent(AuthAgentType.Obex, clientId);

        using var _ = LockAcquisition.AcquireWriteLock(Events);

        if (Events.TryGetValue(clientId, out var authList) && authList != null)
            foreach (var authEvent in authList.Values)
                authEvent.Deny();
    }

    internal static bool AddEvent(
        OperationToken token,
        AuthenticationEvent authenticationEvent,
        AuthAgentType authAgentType = AuthAgentType.None
    )
    {
        using var _ = LockAcquisition.AcquireWriteLock(Events);

        var clientId = Guid.Empty;

        switch (authAgentType)
        {
            case AuthAgentType.Pairing:
                _pairingAgent.Read(out clientId);
                break;

            case AuthAgentType.Obex:
                _obexAgent.Read(out clientId);
                break;

            case AuthAgentType.None:
                clientId = token.ClientId;
                break;
        }

        if (clientId == Guid.Empty)
        {
            authenticationEvent.Deny();
            return false;
        }

        token.ClientId = clientId;

        if (!Events.TryGetValue(clientId, out var authList))
        {
            authList = [];
            Events.Add(clientId, authList);
        }

        return authList.TryAdd(authenticationEvent.CurrentAuthId, authenticationEvent);
    }

    internal static bool SetEventResponse(OperationToken token, long authId, string response)
    {
        using var _ = LockAcquisition.AcquireWriteLock(Events);

        var found = true;

        if (Events.TryGetValue(token.ClientId, out var authList))
        {
            if (authList != null && authList.Remove(authId, out var authEvent))
            {
                authEvent.TryAccept(response);
                found = true;
            }

            if (authList == null || authList?.Count == 0)
                Events.Remove(token.ClientId);
        }

        return found;
    }
}
