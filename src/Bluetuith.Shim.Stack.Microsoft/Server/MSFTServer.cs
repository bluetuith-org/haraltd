using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Bluetuith.Shim.DataTypes.Generic;
using Bluetuith.Shim.DataTypes.OperationToken;
using Bluetuith.Shim.Operations;
using Bluetuith.Shim.Operations.Managers;
using Bluetuith.Shim.Operations.OutputStream;
using H.NotifyIcon.Core;
using Windows.Wdk.System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.WindowsAndMessaging;
using ServerCommand = Bluetuith.Shim.Operations.Commands.Definitions.Server;

namespace Bluetuith.Shim.Stack.Microsoft.Server;

public class MsftServer : IServer
{
    public ErrorData StartServer(string socketPath, string newProcessArgs)
    {
        try
        {
            using var mutex = GetGlobalMutex();
            if (newProcessArgs != "")
            {
                RelaunchProcess(newProcessArgs);
                return Errors.ErrorNone;
            }

            var token = OperationManager.GenerateToken(0, Guid.NewGuid());
            using var trayIcon = new Tray(token, socketPath);

            OperationManager.AddToken(token);

            var error = Monitors.WindowsMonitors.Start(token);
            if (error != Errors.ErrorNone)
            {
                trayIcon.ShowError(error);
                return error;
            }

            Output.OnStarted += s =>
                trayIcon.ShowInfo($"The shim RPC server instance has started at socket '{s}'");

            error = Output.StartSocketServer(socketPath, token);
            OperationManager.CancelAllAsync().Wait();
            if (error != Errors.ErrorNone)
            {
                trayIcon.ShowError(error);
                return error;
            }

            return Errors.ErrorNone;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            PInvoke.MessageBox((HWND)null, ex.Message, IServer.Name, MESSAGEBOX_STYLE.MB_ICONERROR);

            return Errors.ErrorUnexpected.AddMetadata("exception", ex.Message);
        }
        finally
        {
            _ = Monitors.WindowsMonitors.Stop();
        }
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

    public bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public bool Relaunch()
    {
        try
        {
            var process = ProcessUtilities.GetParentProcess();
            if (process.ProcessName != "explorer")
                return false;

            PInvoke.FreeConsole();

            new ServerCommand().Start(null);
        }
        catch
        {
        }

        return true;
    }

    private static void RelaunchProcess(string newProcessArgs)
    {
        var p = new Process();
        p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

        var si = p.StartInfo;
        si.UseShellExecute = true;
        si.CreateNoWindow = true;
        si.FileName = Environment.ProcessPath;
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
            gotHandle = mutex.WaitOne(500, false);
        }
        catch (AbandonedMutexException)
        {
            gotHandle = true;
        }

        if (!gotHandle)
            throw new Exception("Cannot start a new instance. Server is already running.");

        return mutex;
    }
}

internal partial class Tray : IDisposable
{
    private readonly Icon _icon;

    private readonly Stream _iconStream;
    private readonly OperationToken _token;

    internal Tray(OperationToken token, string socketPath)
    {
        _token = token;
        _iconStream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("Bluetuith.Shim.Stack.Microsoft.Resources.shimicon.ico");
        _icon = new Icon(_iconStream);

        var trayIcon = new TrayIconWithContextMenu
        {
            Icon = _icon.Handle,
            ToolTip = $"Bluetuith Shim RPC server\n\nStarted at socket '{socketPath}'",
            ContextMenu = new PopupMenu
            {
                Items = { new PopupMenuItem("Stop", (_, _) => token.Release()) }
            }
        };

        trayIcon.Create();
        Thread.Sleep(1000);

        TrayInstance = trayIcon;
    }

    public TrayIconWithContextMenu TrayInstance { get; }

    public void Dispose()
    {
        _token.Release();

        _icon.Dispose();
        _iconStream.Dispose();

        TrayInstance.Dispose();
    }

    internal void ShowInfo(string info)
    {
        TrayInstance.ShowNotification(nameof(NotificationIcon.Info), info, NotificationIcon.Info);
    }

    internal void ShowError(ErrorData error)
    {
        var errmsg = "An unknown error occurred";
        if (error.Metadata.TryGetValue("exception", out var message))
            errmsg = message.ToString();

        TrayInstance.ShowNotification(
            nameof(NotificationIcon.Error),
            errmsg,
            NotificationIcon.Error
        );
    }
}

internal static class ProcessUtilities
{
    public static unsafe Process GetParentProcess()
    {
        uint returnLength = 0;

        var info = new PROCESS_BASIC_INFORMATION();
        var infoPtr = &info;

        var status = global::Windows.Wdk.PInvoke.NtQueryInformationProcess(
            new HANDLE(Process.GetCurrentProcess().Handle),
            PROCESSINFOCLASS.ProcessBasicInformation,
            infoPtr,
            (uint)Unsafe.SizeOf<PROCESS_BASIC_INFORMATION>(),
            ref returnLength
        );

        if (status.SeverityCode != NTSTATUS.Severity.Success)
            throw new Exception(status.SeverityCode.ToString());

        return Process.GetProcessById((int)info.InheritedFromUniqueProcessId);
    }
}

/*
 * Taken from: https://stackoverflow.com/a/15281070
 */
internal static class ConsoleNativeMethods
{
    // Modified from the original linked answer.
    internal static unsafe void StopProgram()
    {
        var thisPid = Environment.ProcessId;
        var notAttached = 0;

        try
        {
            PInvoke.SetConsoleCtrlHandler(null, true);

            foreach (
                var proc in Process.GetProcessesByName(
                    Path.GetFileNameWithoutExtension(Environment.ProcessPath)
                )
            )
                using (proc)
                {
                    if (proc is null || proc.Id == thisPid)
                        continue;

                    try
                    {
                        if (PInvoke.FreeConsole() && PInvoke.AttachConsole((uint)proc.Id))
                        {
                            if (PInvoke.GenerateConsoleCtrlEvent((uint)CtrlTypes.CtrlCEvent, 0))
                                proc.WaitForExit(2000);
                            else
                                notAttached++;
                        }
                        else
                        {
                            notAttached++;
                        }
                    }
                    catch
                    {
                        notAttached++;
                    }
                }
        }
        catch
        {
            notAttached++;
        }
        finally
        {
            PInvoke.SetConsoleCtrlHandler(null, false);
        }

        if (notAttached > 0)
            throw new Exception("Could not attach to and send signal to processes");
    }

    // Enumerated type for the control messages sent to the handler routine
    private enum CtrlTypes : uint
    {
        CtrlCEvent = 0,
        CtrlBreakEvent,
        CtrlCloseEvent,
        CtrlLogoffEvent = 5,
        CtrlShutdownEvent
    }
}