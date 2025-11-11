using System.Diagnostics;
using System.Runtime.CompilerServices;
using Haraltd.DataTypes.Generic;
using Windows.Wdk.System.Threading;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace Haraltd.Stack.Platform.Windows.Server;

internal static class ProcessUtilities
{
    public static unsafe Process GetParentProcess(out ErrorData error)
    {
        error = Errors.ErrorNone;

        uint returnLength = 0;

        var info = new PROCESS_BASIC_INFORMATION();
        var infoPtr = &info;

        try
        {
            var status = global::Windows.Wdk.PInvoke.NtQueryInformationProcess(
                new HANDLE(Process.GetCurrentProcess().Handle),
                PROCESSINFOCLASS.ProcessBasicInformation,
                infoPtr,
                (uint)Unsafe.SizeOf<PROCESS_BASIC_INFORMATION>(),
                ref returnLength
            );

            if (status.SeverityCode != NTSTATUS.Severity.Success)
            {
                error = Errors.ErrorUnexpected.AddMetadata(
                    "exception",
                    $"Could not get parent process: STATUS is {status.SeverityCode}"
                );
                return null;
            }
        }
        catch (Exception ex)
        {
            error = Errors.ErrorUnexpected.AddMetadata(
                "exception",
                $"Querying for parent process returned an error {ex.Message}"
            );
            return null;
        }

        try
        {
            return Process.GetProcessById((int)info.InheritedFromUniqueProcessId);
        }
        catch
        {
            // ignored
        }

        try
        {
            return Process.GetProcessById((int)info.UniqueProcessId);
        }
        catch
        {
            // ignored
        }

        error = Errors.ErrorUnexpected.AddMetadata("exception", "Could not get parent processes");
        return null;
    }
}
