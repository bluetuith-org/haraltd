using System.Runtime.InteropServices;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Stack.Base;
using Haraltd.Stack.Microsoft.Profiles;
using InTheHand.Net;
using Nefarius.Utilities.DeviceManagement.PnP;
using Windows.Devices.Bluetooth;

namespace Haraltd.Stack.Microsoft.Devices;

public partial class WindowsDevice(BluetoothDevice device, OperationToken token)
    : SafeHandle(-1, true),
        IDeviceController
{
    public BluetoothAddress Address => device.BluetoothAddress;

    private Action<bool> _onDeviceConnectionStateChanged = static delegate { };
    public event Action<bool> OnDeviceConnectionStateChanged
    {
        add
        {
            if (_onDeviceConnectionStateChanged.GetInvocationList().Length == 1)
                device.ConnectionStatusChanged += DeviceOnConnectionStatusChanged;

            _onDeviceConnectionStateChanged += value;
        }
        remove
        {
            _onDeviceConnectionStateChanged -= value;

            if (_onDeviceConnectionStateChanged.GetInvocationList().Length == 1)
                device.ConnectionStatusChanged -= DeviceOnConnectionStatusChanged;
        }
    }

    public override bool IsInvalid => false;

    private void DeviceOnConnectionStatusChanged(BluetoothDevice sender, object args)
    {
        _onDeviceConnectionStateChanged.Invoke(
            sender.ConnectionStatus == BluetoothConnectionStatus.Connected
        );
    }

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

    // Object Push (OPP) based operations
    public async ValueTask<ErrorData> StartFileTransferSessionAsync()
    {
        return await Opp.StartFileTransferSessionAsync(token, Address);
    }

    public (FileTransferModel, ErrorData) QueueFileSend(string filepath)
    {
        return Opp.QueueFileSend(token, Address, filepath);
    }

    public async ValueTask<ErrorData> CancelFileTransferSessionAsync()
    {
        return await Opp.CancelFileTransferSessionAsync(token, Address);
    }

    public async ValueTask<ErrorData> StopFileTransferSessionAsync()
    {
        return await Opp.StopFileTransferSessionAsync(token, Address);
    }

    public async ValueTask<ErrorData> RemoveAsync()
    {
        var (adapter, error) = await WindowsStack.GetDefaultAdapterAsync(token);

        return adapter != null ? await adapter.RemoveDeviceAsync(Address) : error;
    }

    protected override bool ReleaseHandle()
    {
        device.ConnectionStatusChanged -= DeviceOnConnectionStatusChanged;
        device.Dispose();

        return true;
    }
}
