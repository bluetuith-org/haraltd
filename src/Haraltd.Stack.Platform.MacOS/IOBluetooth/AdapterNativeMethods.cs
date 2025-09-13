using IOBluetooth.Shim;
using ObjCRuntime;

namespace IOBluetooth;

public static class AdapterNativeMethods
{
    private const string EmptyAddress = "00:00:00:00:00:00";

    public static int GetPowerState() =>
        IOBluetoothPreferences.IOBluetoothPreferenceGetControllerPowerState();

    public static int GetDiscoverableState() =>
        IOBluetoothPreferences.IOBluetoothPreferenceGetDiscoverableState();

    public static void SetPowerState(int state)
    {
        IOBluetoothPreferences.IOBluetoothPreferenceSetControllerPowerState(state);
    }

    public static void SetDiscoverableState(int state)
    {
        IOBluetoothPreferences.IOBluetoothPreferenceSetDiscoverableState(state);
    }

    public static bool GetHostControllerAddress(out string hostControllerAddress)
    {
        hostControllerAddress = EmptyAddress;

        try
        {
            var address = BluetoothShim.GetHostControllerAddressString();
            if (string.IsNullOrEmpty(address))
                return false;

            hostControllerAddress = address;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string GetActualHostControllerAddress(HostController hostController)
    {
        if (!GetHostControllerAddress(out var hostControllerAddress))
            hostControllerAddress = hostController.AddressAsString;

        return hostControllerAddress;
    }

    public static Guid[] GetLocalServices()
    {
        var services = new List<Guid>();

        var s = IOBluetoothPreferences.IOBluetoothGetLocalServices();
        var ns = Runtime.GetNSObject<NSDictionary>(s);
        if (ns == null)
            return [];

        foreach (var (_, nvalue) in ns)
        {
            if (SdpParser.GetServiceFromSdpRecord(nvalue, out var service))
            {
                services.Add(service);
            }
        }

        return services.ToArray();
    }
}
