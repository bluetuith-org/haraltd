using Bluetuith.Shim.Stack.Models;
using Bluetuith.Shim.Types;
using InTheHand.Net.Bluetooth;
using Nefarius.Utilities.Bluetooth;

namespace Bluetuith.Shim.Stack.Providers.MSFT.Adapters;

public record class AdapterModelExt : AdapterModel
{
    private AdapterModelExt(BluetoothRadio bRadio, bool isAvailable, bool isEnabled)
    {
        Address = bRadio.LocalAddress.ToString("C");
        Name = bRadio.Name;
        Version = bRadio.LmpVersion.ToString();
        Manufacturer = bRadio.Manufacturer.ToString();

        Powered = bRadio.Mode != RadioMode.PowerOff;
        Discoverable = bRadio.Mode == RadioMode.Discoverable;
        Pairable = bRadio.Mode == RadioMode.Connectable;

        IsAvailable = isAvailable;
        IsEnabled = isEnabled;
    }

    public static async Task<(AdapterModel Adapter, ErrorData Error)> ConvertToAdapterModelAsync()
    {
        BluetoothRadio radio;
        AdapterModel adapter = new();

        try
        {
            AdapterMethods.ThrowIfRadioNotOperable();

            using HostRadio hostRadio = new(true);
            radio = BluetoothRadio.Default;
            adapter = new AdapterModelExt(radio, HostRadio.IsAvailable, HostRadio.IsEnabled);
        }
        catch (Exception e)
        {
            return (adapter, StackErrors.ErrorAdapterNotFound.WrapError(new() {
                {"exception", e.Message},
            }));
        }

        return await Task.FromResult((adapter, Errors.ErrorNone));
    }
}