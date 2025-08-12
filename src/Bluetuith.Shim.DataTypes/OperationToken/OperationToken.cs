using System.Collections.Concurrent;

namespace Bluetuith.Shim.DataTypes.OperationToken;

public readonly struct OperationToken : IEquatable<OperationToken>
{
    private readonly Lazy<OperationTokenRef> _reftoken;
    private readonly Action<long> _releaseFunc = delegate { };

    public long OperationId { get; }
    public long RequestId { get; }

    public CancellationTokenSource CancelTokenSource => _reftoken.Value.CancelTokenSource;

    public CancellationTokenSource LinkedCancelTokenSource =>
        _reftoken.Value.LinkedCancelTokenSource;

    public Guid ClientId
    {
        get => _reftoken.Value.ClientId;
        set => _reftoken.Value.ClientId = value;
    }

    public bool HasClientId => _reftoken.Value.HasClientId;

    public static OperationToken None => default;

    public OperationToken(
        long operationId,
        long requestId,
        Action<long> releaseFunc = null,
        bool empty = false
    )
    {
        OperationId = operationId;
        RequestId = requestId;
        _reftoken = !empty ? new Lazy<OperationTokenRef>(() => new OperationTokenRef()) : null;

        if (releaseFunc != null)
            _releaseFunc = releaseFunc;
    }

    public OperationToken(
        long operationId,
        long requestId,
        CancellationToken cancellationToken,
        Action<long> releaseFunc = null
    )
        : this(operationId, requestId, releaseFunc, true)
    {
        _reftoken = new Lazy<OperationTokenRef>(() => new OperationTokenRef(cancellationToken));
    }

    public OperationToken(
        long operationId,
        long requestId,
        Guid clientId,
        CancellationToken cancellationToken = default,
        Action<long> releaseFunc = null
    )
        : this(operationId, requestId, releaseFunc, true)
    {
        _reftoken = new Lazy<OperationTokenRef>(() =>
            new OperationTokenRef(clientId, cancellationToken)
        );
    }

    public void Release()
    {
        if (IsReleased())
            return;

        _releaseFunc(OperationId);
        ReleaseInternal();
    }

    public void ReleaseInternal()
    {
        _reftoken.Value.Dispose();
    }

    public bool Wait()
    {
        return _reftoken.Value.Wait();
    }

    public bool ReleaseAfter(int timeout)
    {
        return _reftoken.Value.ReleaseAfter(timeout);
    }

    public bool IsReleased()
    {
        return _reftoken.Value.IsReleased();
    }

    public static bool operator ==(OperationToken left, OperationToken right)
    {
        return left.OperationId.Equals(right.OperationId);
    }

    public static bool operator !=(OperationToken left, OperationToken right)
    {
        return !left.OperationId.Equals(right.OperationId);
    }

    public override bool Equals(object obj)
    {
        return obj is OperationToken token && Equals(token);
    }

    bool IEquatable<OperationToken>.Equals(OperationToken other)
    {
        return Equals(other);
    }

    public override int GetHashCode()
    {
        return OperationId.GetHashCode();
    }
}

internal class OperationTokenRef : IDisposable
{
    private readonly CancellationTokenSource _cancelTokenSource;
    private readonly ConcurrentBag<CancellationTokenSource> _linkedTokens = [];
    private bool _isDisposed;

    internal OperationTokenRef()
    {
        _cancelTokenSource = new CancellationTokenSource();
    }

    internal OperationTokenRef(Guid clientId, CancellationToken cancelToken = default)
    {
        if (clientId == Guid.Empty)
            throw new ArgumentNullException(nameof(clientId));

        ClientId = clientId;

        _cancelTokenSource =
            cancelToken != CancellationToken.None
                ? CancellationTokenSource.CreateLinkedTokenSource(cancelToken)
                : new CancellationTokenSource();
    }

    internal OperationTokenRef(CancellationToken cancelToken)
    {
        _cancelTokenSource =
            cancelToken == CancellationToken.None
                ? throw new ArgumentNullException(nameof(cancelToken))
                : CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
    }

    internal Guid ClientId { get; set; }
    internal bool HasClientId => ClientId != Guid.Empty;

    internal CancellationTokenSource CancelTokenSource =>
        !_isDisposed
            ? _cancelTokenSource
            : throw new NullReferenceException(nameof(CancelTokenSource));

    internal CancellationTokenSource LinkedCancelTokenSource
    {
        get
        {
            if (_isDisposed)
                throw new NullReferenceException(nameof(LinkedCancelTokenSource));

            var cancelToken = CancellationTokenSource.CreateLinkedTokenSource(
                _cancelTokenSource.Token
            );
            _linkedTokens.Add(cancelToken);

            return cancelToken;
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        try
        {
            foreach (var token in _linkedTokens)
            {
                token?.Cancel();
                token?.Dispose();
            }

            _cancelTokenSource?.Cancel();
            _cancelTokenSource?.Dispose();
        }
        catch
        {
        }
    }

    internal bool Wait()
    {
        if (!_isDisposed)
            _cancelTokenSource.Token.WaitHandle.WaitOne();

        return !_isDisposed;
    }

    internal bool ReleaseAfter(int timeout)
    {
        if (!_isDisposed)
            _cancelTokenSource.CancelAfter(timeout);

        return !_isDisposed;
    }

    internal bool IsReleased()
    {
        return _isDisposed;
    }
}