using Nefarius.Utilities.DeviceManagement.PnP;

namespace Haraltd.Stack.Microsoft.Windows;

internal static class WindowsPnPInformation
{
    internal static Guid BluetoothRadiosInterfaceGuid =>
        Guid.Parse("{92383b0e-f90e-4ac9-8d44-8c2d0d0ebda2}");

    internal static Guid BluetoothDevicesInterfaceGuid =>
        Guid.Parse("{00f40965-e89d-4487-9890-87c3abb211f4}");

    internal static Guid HfEnumeratorInterfaceGuid =>
        Guid.Parse("{bd41df2d-addd-4fc9-a194-b9881d2a2efa}");

    internal static string HfEnumeratorRegistryKey =>
        $@"{DeviceClassesRegistryPath}\{{{HfEnumeratorInterfaceGuid}}}";

    internal static string DeviceClassesRegistryPath =>
        @"SYSTEM\CurrentControlSet\Control\DeviceClasses";

    internal static class Adapter
    {
        internal static string ServicesRegistryPath =>
            @"SYSTEM\CurrentControlSet\Services\BTHPORT\Parameters\Services";

        internal static string DiscoverableRegistryKey => "Write Scan Enable";

        internal static string RadioStateRegistryKey => "RadioState";

        internal static DevicePropertyKey Address =>
            CustomDeviceProperty.CreateCustomDeviceProperty(
                Guid.Parse("{A92F26CA-EDA7-4B1D-9DB2-27B68AA5A2EB}"),
                1,
                typeof(ulong)
            );

        internal static string DeviceParametersRegistryPath(string deviceId)
        {
            return $@"SYSTEM\CurrentControlSet\Enum\{deviceId}\Device Parameters";
        }
    }

    internal static class Device
    {
        internal static Guid DevpkeyBluetoothPropertiesGuid =>
            Guid.Parse("{2bd67d8b-8beb-48d5-87e0-6cda3428040a}");

        internal static DevicePropertyKey Name => DevicePropertyKey.NAME;

        internal static DevicePropertyKey AepId =>
            CustomDeviceProperty.CreateCustomDeviceProperty(
                Guid.Parse("{3B2CE006-5E61-4FDE-BAB8-9B8AAC9B26DF}"),
                8,
                typeof(string)
            );

        internal static DevicePropertyKey Class =>
            CustomDeviceProperty.CreateCustomDeviceProperty(
                DevpkeyBluetoothPropertiesGuid,
                10,
                typeof(uint)
            );

        internal static DevicePropertyKey IsConnected =>
            CustomDeviceProperty.CreateCustomDeviceProperty(
                Guid.Parse("{83DA6326-97A6-4088-9453-A1923F573B29}"),
                15,
                typeof(bool)
            );

        internal static DevicePropertyKey BatteryPercentage =>
            CustomDeviceProperty.CreateCustomDeviceProperty(
                Guid.Parse("{104EA319-6EE2-4701-BD47-8DDBF425BBE5}"),
                2,
                typeof(byte)
            );

        internal static string ServicesRegistryPath(string adapterAddress, string deviceAddress)
        {
            adapterAddress = adapterAddress.Replace(":", "");
            deviceAddress = deviceAddress.Replace(":", "");

            return $@"SYSTEM\CurrentControlSet\Services\BTHPORT\Parameters\Devices\{deviceAddress}\ServicesFor{adapterAddress}";
        }
    }
}

internal static class PnPUtils
{
    internal static bool GetAddressFromInterfaceId(string interfaceId, out string address)
    {
        address = "";

        try
        {
            var addr = interfaceId[(interfaceId.LastIndexOf('&') + 1)..][..12];
            if (addr.Length != 12)
                return false;

            address = string.Join(
                ":",
                Enumerable.Range(0, 6).Select(i => addr.Substring(i * 2, 2).ToLower()).ToArray()
            );
        }
        catch
        {
            return false;
        }

        return true;
    }
}
