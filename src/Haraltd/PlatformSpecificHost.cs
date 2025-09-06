using Haraltd.Operations;
using Haraltd.Stack.Base;

namespace Haraltd;

public class PlatformSpecificHost : IOperationHost
{
    public IBluetoothStack Stack { get; } =
#if WINDOWS10_0_19041_0_OR_GREATER
        new Haraltd.Stack.Microsoft.WindowsStack();
#else
        null;
#endif

    public IServer Server { get; } =
#if WINDOWS10_0_19041_0_OR_GREATER
    new Haraltd.Stack.Microsoft.Server.WindowsServer();
#else
        null;
#endif
}