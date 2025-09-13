using System.Runtime.InteropServices;

namespace IOBluetooth;

// ReSharper disable once InconsistentNaming
static partial class IOBluetoothPreferences
{
    // ReSharper disable once InconsistentNaming
    private const string IOBluetoothLibraryName =
        "/System/Library/Frameworks/IOBluetooth.framework/IOBluetooth";

    [LibraryImport(IOBluetoothLibraryName)]
    public static partial int IOBluetoothPreferenceGetControllerPowerState();

    [LibraryImport(IOBluetoothLibraryName)]
    public static partial void IOBluetoothPreferenceSetControllerPowerState(int powerState);

    [LibraryImport(IOBluetoothLibraryName)]
    public static partial int IOBluetoothPreferenceGetDiscoverableState();

    [LibraryImport(IOBluetoothLibraryName)]
    public static partial void IOBluetoothPreferenceSetDiscoverableState(int discoverableState);

    [LibraryImport(IOBluetoothLibraryName)]
    public static unsafe partial nint IOBluetoothGetLocalServices();
}
