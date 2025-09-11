using Haraltd.Stack.Base;

namespace Haraltd.Operations;

public interface IOperationHost
{
    public IBluetoothStack Stack { get; }
    public IServer Server { get; }
}

public static class OperationHost
{
    private static IOperationHost _host;
    public static IOperationHost Instance =>
        _host ?? throw new NullReferenceException("The host was not initialized.");

    public static void Register(IOperationHost host) => _host = host;
}
