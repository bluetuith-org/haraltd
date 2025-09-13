using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using IOBluetooth;

namespace Haraltd.Stack.Platform.MacOS.Adapters;

public static class MacOsAdapterExtensions
{
    public static (AdapterModel, ErrorData) ToAdapterModel(
        this HostController adapter,
        string address
    )
    {
        try
        {
            var powered = AdapterNativeMethods.GetPowerState() > 0;
            var discoverable = AdapterNativeMethods.GetDiscoverableState() > 0;
            var pairable = powered;
            var name = adapter.NameAsString;

            return (
                new AdapterModel
                {
                    Address = address,
                    OptionPowered = powered,
                    OptionDiscoverable = discoverable,
                    OptionPairable = pairable,

                    // Set
                    OptionDiscovering = false,
                    Name = name,
                    Alias = name,
                    UniqueName = name,
                    UuiDs = AdapterNativeMethods.GetLocalServices(),
                },
                Errors.ErrorNone
            );
        }
        catch (Exception e)
        {
            return (
                new AdapterModel(),
                Errors.ErrorAdapterNotFound.AddMetadata("exception", e.Message)
            );
        }
    }
}
