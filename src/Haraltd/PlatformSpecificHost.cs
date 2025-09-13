using Haraltd.Operations;
using Haraltd.Stack.Base;

namespace Haraltd;

public class PlatformSpecificHost : IOperationHost
{
    public IBluetoothStack Stack { get; } =
#if WINDOWS
        new Haraltd.Stack.Microsoft.WindowsStack();
#elif OSX
        new Haraltd.Stack.Platform.MacOS.MacOsStack();
#else
        throw new PlatformNotSupportedException();
#endif

    public IServer Server { get; } =
#if WINDOWS
        new Haraltd.Stack.Microsoft.Server.WindowsServer();
#elif OSX
        new Haraltd.Stack.Platform.MacOS.Server.MacOsServer();
#else
        throw new PlatformNotSupportedException();
#endif
}
