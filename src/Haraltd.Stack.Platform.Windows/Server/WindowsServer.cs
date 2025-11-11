using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.Commands;
using Haraltd.Operations.Managers;
using Haraltd.Operations.OutputStream;
using Haraltd.Stack.Base;
using Haraltd.Stack.Platform.Windows.Monitors;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Haraltd.Stack.Platform.Windows.Server;

public partial class WindowsServer : IServer
{
    private readonly Tray _tray = new();
    private bool _disposed;

    public bool IsRunning { get; private set; }
    public string RootCacheDirectory =>
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    private Mutex _mutex;

    public ErrorData StartServer(OperationToken token, string socketPath, string parameter)
    {
        try
        {
            _mutex = GetGlobalMutex();
            if (parameter != "")
            {
                RelaunchProcess(parameter);
                return Errors.ErrorNone;
            }

            _tray.Create(token, (this as IServer).SocketPath);

            var error = WindowsMonitors.Start(token);
            if (error != Errors.ErrorNone)
            {
                return _tray.ShowError(error);
            }

            error = Output.StartSocketServer(socketPath, token);
            if (error != Errors.ErrorNone)
            {
                return _tray.ShowError(error);
            }

            OperationManager.SetOperationProperties(token);
            IsRunning = true;

            _tray.ShowInfo("Haraltd is running");

            return Errors.ErrorNone;
        }
        catch (Exception ex)
        {
            PInvoke.MessageBox((HWND)null, ex.Message, IServer.Name, MESSAGEBOX_STYLE.MB_ICONERROR);

            return Errors.ErrorUnexpected.AddMetadata("exception", ex.Message);
        }
    }

    public bool TryRelaunch(OperationToken token, string[] args, out ErrorData err)
    {
        err = Errors.ErrorNone;

        if (args is { Length: > 0 } || (args.Length == 0 && !IsLaunchedFromGui(out err)))
        {
            return false;
        }

        PInvoke.FreeConsole();
        StartServer(token, (this as IServer).SocketPath, "server start -t");

        return true;
    }

    public ErrorData ShouldContinueExecution(string[] args)
    {
        if (IsLaunchedFromGui(out var error))
        {
            return error;
        }

        if (!IsAdministrator())
        {
            error = Errors.ErrorUnsupported.AddMetadata(
                "exception",
                "This program should be run as administrator"
            );
        }

        return error;
    }

    public ErrorData LaunchCurrentInstance(OperationToken token, string[] args)
    {
        var parserContext = new CommandParserContext(token);

        Task.Run(() =>
        {
            OperationManager.ExecuteHandler(token, args, parserContext);
        });

        if (!parserContext.IsParsed)
            return parserContext.Error;

        parserContext.ContinueCommandExecution();

        token.Wait();
        StopCurrentInstance();

        return parserContext.Error;
    }

    public void StopCurrentInstance()
    {
        OperationManager.CancelAllAsync().Wait();
    }

    public ErrorData StopServer(string newProcessArgs)
    {
        try
        {
            if (newProcessArgs != "")
            {
                RelaunchProcess(newProcessArgs);
                return Errors.ErrorNone;
            }

            ConsoleNativeMethods.StopProgram();
        }
        catch (Exception ex)
        {
            return Errors.ErrorUnexpected.AddMetadata("exception", ex.Message);
        }

        return Errors.ErrorNone;
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static bool IsLaunchedFromGui(out ErrorData error)
    {
        error = Errors.ErrorNone;

        using var process = ProcessUtilities.GetParentProcess(out error);
        if (process == null)
            return false;

        return process.ProcessName == "explorer";
    }

    private static void RelaunchProcess(string newProcessArgs)
    {
        var p = new Process();
        p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

        var si = p.StartInfo;
        si.UseShellExecute = true;
        si.CreateNoWindow = true;
        si.FileName = Environment.ProcessPath!;
        si.Verb = "runas";
        si.Arguments = newProcessArgs;

        p.Start();
    }

    private static Mutex GetGlobalMutex()
    {
        bool gotHandle;
        var executingAssembly = Assembly.GetExecutingAssembly();
        var customAttributes = executingAssembly.GetCustomAttributes(typeof(GuidAttribute), false);
        var guidAttribute = ((GuidAttribute)customAttributes[0]).Value;
        var mutexId = $"Global\\{{{guidAttribute}}}";

        var mutex = new Mutex(false, mutexId, out _);
        try
        {
            gotHandle = mutex.WaitOne(1000, false);
        }
        catch (AbandonedMutexException)
        {
            gotHandle = true;
        }

        if (!gotHandle)
            throw new Exception("Cannot start a new instance. Server is already running.");

        return mutex;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _ = WindowsMonitors.Stop();
        Output.Close();
        _tray.Dispose();

        _mutex?.Dispose();

        GC.SuppressFinalize(this);
    }
}
