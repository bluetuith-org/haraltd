using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Stack.Base;
using Haraltd.Stack.Base.Profiles.Opp;
using Haraltd.Stack.Platform.Windows.Profiles;
using InTheHand.Net;
using Nefarius.Utilities.DeviceManagement.PnP;
using Windows.Devices.Bluetooth;

namespace Haraltd.Stack.Platform.Windows.Devices;

public partial class WindowsDevice(BluetoothDevice device, OperationToken token) : IDeviceController
{
    public BluetoothAddress Address => device.BluetoothAddress;

    public (DeviceModel, ErrorData) Properties()
    {
        return ConvertToDeviceModel(Address);
    }

    public (DeviceModel, ErrorData) GetDevice()
    {
        return ConvertToDeviceModel(Address);
    }

    internal static (DeviceModel, ErrorData) ConvertToDeviceModel(BluetoothAddress address)
    {
        DeviceModel device = new();

        return (device, device.MergeDeviceByAddress(address));
    }

    internal static (DeviceModel, ErrorData) ConvertToDeviceModel(
        PnPDevice pnpDevice,
        OperationToken token
    )
    {
        DeviceModel device = new();

        return (device, device.MergePnpDevice(pnpDevice, token));
    }

    public async ValueTask<ErrorData> ConnectAsync()
    {
        return await Connection.ConnectAsync(token, Address);
    }

    public async ValueTask<ErrorData> ConnectProfileAsync(Guid profileGuid)
    {
        return await Connection.ConnectProfileAsync(token, Address, profileGuid);
    }

    public async ValueTask<ErrorData> DisconnectAsync()
    {
        var (adapter, error) = await WindowsStack.GetDefaultAdapterAsync(token);

        return adapter != null ? await adapter.DisconnectDeviceAsync(Address) : error;
    }

    public async ValueTask<ErrorData> DisconnectProfileAsync(Guid profileGuid)
    {
        return await DisconnectAsync();
    }

    public Task<ErrorData> PairAsync(int timeout = 10)
    {
        return new Pairing().PairAsync(token, Address, timeout);
    }

    public ErrorData CancelPairing()
    {
        return Pairing.CancelPairing(Address);
    }

    // Advanced Audio Distribution (A2DP) based operations
    public async ValueTask<ErrorData> StartAudioSessionAsync()
    {
        return await A2dp.StartAudioSessionAsync(token, Address);
    }

    public DeviceOppManager GetOppManager()
    {
        return new DeviceOppManager(WindowsStack.OppSessionManager, token, Address);
    }

    public async ValueTask<ErrorData> RemoveAsync()
    {
        var (adapter, error) = await WindowsStack.GetDefaultAdapterAsync(token);

        return adapter != null ? await adapter.RemoveDeviceAsync(Address) : error;
    }

    public void Dispose()
    {
        device?.Dispose();
        GC.SuppressFinalize(this);
    }
}
