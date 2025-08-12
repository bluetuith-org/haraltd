using System.Collections.Concurrent;
using Bluetuith.Shim.DataTypes.OperationToken;
using Bluetuith.Shim.Operations.Commands;

namespace Bluetuith.Shim.Operations.Managers;

public static class OperationManager
{
    private static readonly ConcurrentDictionary<long, OperationInstance> Operations = new();
    private static long _operationId;

    public static void ExecuteHandler(OperationToken token, string[] args)
    {
        if (Operations.TryAdd(token.OperationId, new OperationInstance(token, false, false)))
            CommandParser.Parse(token, args);
    }

    public static bool GetToken(long operationId, out OperationToken token)
    {
        token = OperationToken.None;

        if (Operations.TryGetValue(operationId, out var instance))
        {
            token = instance.Token;
            return true;
        }

        return false;
    }

    public static void RemoveToken(OperationToken token)
    {
        if (Operations.TryGetValue(token.OperationId, out var instance) && !instance.IsTask)
            Cancel(token.OperationId);
    }

    public static void Cancel(long operationId)
    {
        if (Operations.TryRemove(operationId, out var instance))
            instance.Token.ReleaseInternal();
    }

    public static async Task CancelAllAsync()
    {
        foreach (var instance in Operations.Values)
            instance.Token.ReleaseInternal();

        await Task.Delay(100);
    }

    public static void CancelClientOperations(Guid clientId)
    {
        foreach (var instance in Operations.Values)
            if (
                instance.CancelWhenDisconnected
                && instance.Token.HasClientId
                && instance.Token.ClientId == clientId
            )
                instance.Token.Release();
    }

    public static void SetOperationProperties(
        OperationToken token,
        bool isExtendedTask = true,
        bool cancelOnClientDisconnect = false
    )
    {
        if (Operations.TryGetValue(token.OperationId, out var instance))
        {
            instance.IsTask = isExtendedTask;
            instance.CancelWhenDisconnected = cancelOnClientDisconnect;
        }
    }

    public static void WaitForExtendedOperations()
    {
        foreach (var instance in Operations.Values)
            if (instance.IsTask)
                instance.Token.Wait();
    }

    public static OperationToken GenerateToken(
        long requestId,
        Guid clientId,
        CancellationToken token = default
    )
    {
        var operationId = ++_operationId;

        return new OperationToken(operationId, requestId, clientId, token, Remove);
    }

    public static OperationToken GenerateToken(long requestId, CancellationToken token = default)
    {
        var operationId = ++_operationId;

        return token == CancellationToken.None
            ? new OperationToken(operationId, requestId)
            : new OperationToken(operationId, requestId, token, Remove);
    }

    public static void AddToken(OperationToken token)
    {
        Operations.TryAdd(token.OperationId, new OperationInstance(token, false, false));
    }

    public static void Remove(long operationId)
    {
        Operations.TryRemove(operationId, out _);
    }

    private class OperationInstance(
        OperationToken newToken,
        bool markedAsTask,
        bool cancelOnDisconnect
    )
    {
        public readonly OperationToken Token = newToken;
        public bool CancelWhenDisconnected = cancelOnDisconnect;
        public bool IsTask = markedAsTask;
    }
}

public class ParseResult
{
    internal async Task InvokeAsync(CancellationToken token)
    {
        await Task.CompletedTask;
    }
}