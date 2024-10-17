using System.Collections.Concurrent;
using System.CommandLine;

namespace Bluetuith.Shim.Executor;

public static class Operation
{
    private static int _currentOperationId = 0;
    private static readonly ConcurrentDictionary<int, OperationToken> operations = new();

    public static async Task ExecuteAsync(OperationToken token, ParseResult parsed)
    {
        token.OperationName = parsed.CommandResult.Command.Name;
        if (operations.TryAdd(token.OperationId, token))
        {
            using (token)
            {
                await parsed.InvokeAsync(token.CancelTokenSource.Token);
                operations.TryRemove(token.OperationId, out _);
            }
        }
    }

    public static async Task CancelAsync(int operationId)
    {
        if (operations.TryRemove(operationId, out OperationToken token))
        {
            await token.CancelTokenSource.CancelAsync();
        }
    }

    public static async Task CancelAllAsync()
    {
        foreach (OperationToken token in operations.Values)
        {
            await token.CancelTokenSource.CancelAsync();
        }
    }

    public static OperationToken GenerateToken(int requestId)
    {
        return new OperationToken(++_currentOperationId, requestId, new());
    }
}

public record struct OperationToken : IEquatable<OperationToken>, IComparable<OperationToken>, IDisposable
{
    public int OperationId { get; }
    public int RequestId { get; }
    public string OperationName { get; set; }

    private CancellationTokenSource _cancelTokenSource;
    public CancellationTokenSource CancelTokenSource
    {
        get => _cancelTokenSource ?? throw new ArgumentException("OperationToken is empty");
        private set => _cancelTokenSource = value;
    }
    public CancellationToken CancelToken => CancelTokenSource.Token;

    public static OperationToken Empty
    {
        get
        {
            CancellationTokenSource cancelToken = new();
            cancelToken.Cancel();

            return new OperationToken(-1, -1, cancelToken);
        }
    }

    public OperationToken(string name, int operationId, int requestId, CancellationTokenSource cancelTokenSource)
    {
        OperationName = name;
        OperationId = operationId;
        RequestId = requestId;
        _cancelTokenSource = cancelTokenSource;
    }

    public OperationToken(int operationId, int requestId, CancellationTokenSource cancelTokenSource) :
        this("", operationId, requestId, cancelTokenSource)
    { }

    public static OperationToken GetLinkedToken(int operationId, int requestId, CancellationToken cancelToken)
    {
        return new OperationToken(operationId, requestId, CancellationTokenSource.CreateLinkedTokenSource(cancelToken));
    }

    public bool Equals(OperationToken token)
    {
        return CompareTo(token) == 0;
    }

    public int CompareTo(OperationToken other)
    {
        return OperationId.CompareTo(other.OperationId);
    }

    public override int GetHashCode()
    {
        return OperationId.GetHashCode();
    }

    public void Dispose()
    {
        _cancelTokenSource?.Cancel();
        _cancelTokenSource?.Dispose();
    }

    public static bool operator <(OperationToken left, OperationToken right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(OperationToken left, OperationToken right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >(OperationToken left, OperationToken right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(OperationToken left, OperationToken right)
    {
        return left.CompareTo(right) >= 0;
    }
}