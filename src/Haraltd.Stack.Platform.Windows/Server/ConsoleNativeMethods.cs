using System.Diagnostics;
using Windows.Win32;

namespace Haraltd.Stack.Platform.Windows.Server;

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
        CtrlShutdownEvent,
    }
}
