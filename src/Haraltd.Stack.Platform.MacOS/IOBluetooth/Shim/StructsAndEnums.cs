using System.Runtime.InteropServices;
using ObjCRuntime;

namespace IOBluetooth.Shim;

[StructLayout(LayoutKind.Sequential)]
public struct AdapterEventData
{
    public int Discoverable;

    public int Powered;

    public int Discovering;
}

[Native]
public enum EventAction : ulong
{
    Added = 1,
    Updated = 2,
    Removed = 3,
}

[StructLayout(LayoutKind.Sequential)]
public struct DeviceBatteryInfo
{
    public bool modified;

    public int BatteryPercentSingle;

    public int BatteryPercentCombined;

    public int BatteryPercentLeft;

    public int BatteryPercentRight;
}

[StructLayout(LayoutKind.Sequential)]
public struct DeviceEventData
{
    public DeviceBatteryInfo BatteryInfo;
}
