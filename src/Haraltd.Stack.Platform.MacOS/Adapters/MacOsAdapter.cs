using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Stack.Base;
using Haraltd.Stack.Base.Profiles.Opp;
using Haraltd.Stack.Platform.MacOS.Devices;
using Haraltd.Stack.Platform.MacOS.Monitors;
using InTheHand.Net;
using IOBluetooth;
using IOBluetooth.Shim;

namespace Haraltd.Stack.Platform.MacOS.Adapters;

internal class MacOsAdapter(HostController adapter, OperationToken token) : IAdapterController
{
    public BluetoothAddress Address =>
        BluetoothAddress.Parse(AdapterNativeMethods.GetActualHostControllerAddress(adapter));

    public (AdapterModel, ErrorData) Properties()
    {
        var addr = AdapterNativeMethods.GetActualHostControllerAddress(adapter);

        return adapter.ToAdapterModel(addr);
    }

    public ErrorData SetPoweredState(bool enable)
    {
        AdapterNativeMethods.SetPowerState(enable ? 1 : 0);

        return Errors.ErrorNone;
    }

    public ErrorData SetPairableState(bool enable)
    {
        return Errors.ErrorUnsupported;
    }

    public ErrorData SetDiscoverableState(bool enable)
    {
        AdapterNativeMethods.SetDiscoverableState(enable ? 1 : 0);

        return Errors.ErrorNone;
    }

    public (GenericResult<List<DeviceModel>>, ErrorData) GetPairedDevices()
    {
        var pairedDevices = new List<DeviceModel>();
        var error = Errors.ErrorNone;

        var devicesResult = BluetoothShim.GetPairedDevices();
        if (devicesResult.Error != null)
        {
            error = Errors.ErrorUnexpected.AddMetadata(
                "exception",
                devicesResult.Error.Description
            );
            goto ReturnResult;
        }

        foreach (var device in devicesResult.Value)
        {
            (var d, error) = device.ToDeviceModel();
            if (error != Errors.ErrorNone)
                break;

            pairedDevices.Add(d);
        }

        ReturnResult:
        return (pairedDevices.ToResult("Paired Devices:", "paired_devices"), error);
    }

    public Guid[] GetServices()
    {
        return AdapterNativeMethods.GetLocalServices();
    }

    public AdapterOppManager GetOppManager()
    {
        return new AdapterOppManager(MacOsStack.OppSessionManager, token, Address);
    }

    public ErrorData StartDeviceDiscovery(int timeout = 0)
    {
        return DiscoveryMonitor.StartForClient(token, timeout);
    }

    public ErrorData StopDeviceDiscovery()
    {
        return DiscoveryMonitor.StopForClient(token);
    }

    public async ValueTask<ErrorData> DisconnectDeviceAsync(BluetoothAddress address)
    {
        await ValueTask.CompletedTask;

        using var device = BluetoothDevice.DeviceWithAddressString(address.ToString("C"));
        if (device == null)
            return Errors.ErrorDeviceNotFound;

        return device.CloseConnection() > 0 ? Errors.ErrorDeviceDisconnect : Errors.ErrorNone;
    }

    public async Task<ErrorData> RemoveDeviceAsync(BluetoothAddress address)
    {
        await ValueTask.CompletedTask;

        using var device = BluetoothDevice.DeviceWithAddressString(address.ToString("C"));
        if (device == null)
            return Errors.ErrorDeviceNotFound;

        BluetoothShim.RemoveKnownDevice(device);
        return Errors.ErrorNone;
    }

    public void Dispose()
    {
        adapter.Dispose();
    }
}
