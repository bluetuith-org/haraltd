using System.Diagnostics;
using System.Runtime.InteropServices;
using CoreFoundation;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.Commands;
using Haraltd.Operations.Managers;
using Haraltd.Operations.OutputStream;
using Haraltd.Stack.Base;
using Haraltd.Stack.Platform.MacOS.Monitors;
using IOBluetooth.Shim;

namespace Haraltd.Stack.Platform.MacOS.Server;

public class MacOsServer : IServer
{
    private readonly CFRunLoop _runloop = CFRunLoop.Main;
    private bool _disposed;

    public bool IsRunning { get; private set; }

    public string RootCacheDirectory =>
        Environment.GetFolderPath(Environment.SpecialFolder.InternetCache);

    public ErrorData StartServer(OperationToken token, string socketPath, string parameter)
    {
        if (IsRunning)
            return Errors.ErrorOperationInProgress;

        var error = Errors.ErrorNone;

        try
        {
            error = ServerUtilities.CheckIfUniqueInstance();
            if (error != Errors.ErrorNone)
                goto ShowError;

            if (!string.IsNullOrEmpty(parameter))
            {
                error = ServerUtilities.RelaunchProcess(parameter);
                goto ShowError;
            }

            error = MacOsMonitors.Start(token);
            if (error != Errors.ErrorNone)
                goto ShowError;

            error = Output.StartSocketServer(socketPath, token);
            if (error != Errors.ErrorNone)
            {
                OperationManager.CancelAllAsync().Wait();
                goto ShowError;
            }

            OperationManager.SetOperationProperties(token);
            IsRunning = true;
        }
        catch (Exception ex)
        {
            error = Errors.ErrorUnexpected.AddMetadata("exception", ex.Message);
        }

        ShowError:
        return error;
    }

    public ErrorData StopServer(string parameter)
    {
        if (!string.IsNullOrEmpty(parameter))
        {
            var err = ServerUtilities.RelaunchProcess(parameter);
            return err != Errors.ErrorNone ? err : Errors.ErrorNone;
        }

        if (!ServerUtilities.GetThisApplicationInstances(out var instances, out var error))
            return error;

        try
        {
            foreach (var instance in instances)
            {
                instance.Terminate();
            }
        }
        catch
        {
            return Errors.ErrorUnexpected.AddMetadata("exception", "Could not stop instances");
        }

        return Errors.ErrorNone;
    }

    public ErrorData LaunchCurrentInstance(OperationToken token, string[] args)
    {
        var error = Errors.ErrorNone;
        var parserContext = new CommandParserContext(token);

        Task.Run(() =>
        {
            OperationManager.ExecuteHandler(token, args, parserContext);
            token.Wait();

            if (parserContext is { IsParsed: true, IsServerCommand: false })
                _runloop.Stop();
        });

        if (!parserContext.IsParsed)
            return parserContext.Error;

        var res = BluetoothShim.GetBluetoothPermissionStatus();
        if (res.Error != null || !res.Value.BoolValue)
            error = Errors.ErrorUnsupported.AddMetadata(
                "exception",
                "Bluetooth permission status was not granted"
            );

        var nsError = BluetoothShim.TryInitCoordinator();
        if (nsError != null)
            error = Errors.ErrorUnsupported.AddMetadata("exception", nsError.LocalizedDescription);

        if (
            error == Errors.ErrorNone
            && parserContext is { IsParsed: true, IsServerCommand: false }
        )
        {
            parserContext.ContinueCommandExecution();
            _runloop.Run();
            return parserContext.Error;
        }

        if (error == Errors.ErrorNone)
        {
            parserContext.ContinueCommandExecution();
            error = parserContext.Error;
        }

        if (!parserContext.IsServerCommand || error == Errors.ErrorNone && token.IsReleased())
            return error;

        try
        {
            NSApplication.CheckForIllegalCrossThreadCalls = false;
            NSApplication.SharedApplication.Delegate = new AppDelegate(
                NSApplication.SharedApplication,
                error,
                this,
                token
            );

            NSApplication.SharedApplication.Run();
        }
        catch (Exception ex)
        {
            error = Errors.ErrorUnexpected.AddMetadata("exception", ex.Message);
        }
        finally
        {
            StopCurrentInstance();
        }

        return error;
    }

    public void StopCurrentInstance()
    {
        OperationManager.CancelAllAsync().Wait();
    }

    public bool TryRelaunch(OperationToken token, string[] args, out ErrorData err)
    {
        err = Errors.ErrorNone;

        if (
            args is { Length: > 0 }
            || (args.Length == 0 && !ServerUtilities.IsParentProcessLaunchd())
        )
        {
            err = Errors.ErrorNone;
            return false;
        }

        StartServer(token, (this as IServer).SocketPath, "server start -t");

        return true;
    }

    public ErrorData ShouldContinueExecution(string[] args)
    {
        return ServerUtilities.IsRoot()
            ? Errors.ErrorUnsupported.AddMetadata("exception", "Cannot run as root")
            : Errors.ErrorNone;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        _disposed = true;

        if (disposing)
        {
            _ = MacOsMonitors.Stop();
            Output.Close();

            _runloop?.Dispose();
        }
    }
}

internal static partial class ServerUtilities
{
    [LibraryImport("libc")]
    private static partial uint geteuid();

    [LibraryImport("libc")]
    private static partial int getppid();

    internal static bool IsRoot()
    {
        return geteuid() == 0;
    }

    internal static bool GetThisApplicationInstances(
        out NSRunningApplication[] apps,
        out ErrorData error
    )
    {
        apps = [];
        error = Errors.ErrorNone;

        try
        {
            apps = NSRunningApplication.GetRunningApplications(
                NSBundle.MainBundle.BundleIdentifier
            );
            return true;
        }
        catch (Exception ex)
        {
            error = Errors.ErrorUnexpected.AddMetadata("exception", ex.Message);
            return false;
        }
    }

    internal static ErrorData RelaunchProcess(string parameters)
    {
        try
        {
            var p = new Process();
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            var si = p.StartInfo;
            si.UseShellExecute = true;
            si.CreateNoWindow = true;
            si.FileName = Environment.ProcessPath;
            si.Arguments = parameters;

            p.Start();
        }
        catch (Exception ex)
        {
            return Errors.ErrorUnexpected.AddMetadata("exception", ex.Message);
        }

        return Errors.ErrorNone;
    }

    internal static ErrorData CheckIfUniqueInstance()
    {
        if (!GetThisApplicationInstances(out var instances, out var error))
        {
            return error;
        }

        if (instances.Length > (IsParentProcessLaunchd() ? 1 : 0))
        {
            error = Errors.ErrorUnsupported.AddMetadata(
                "exception",
                "Only one instance is supported."
            );
        }

        return error;
    }

    internal static bool IsParentProcessLaunchd()
    {
        return getppid() == 1;
    }
}
