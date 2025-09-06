using System.Runtime.InteropServices;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Stack.Base;
using Haraltd.Stack.Base.Sockets;
using Haraltd.Stack.Microsoft.Adapters;
using Haraltd.Stack.Microsoft.Devices;
using Haraltd.Stack.Microsoft.Sockets;
using InTheHand.Net;
using Windows.Devices.Bluetooth;
using static Haraltd.DataTypes.Models.Features;

namespace Haraltd.Stack.Microsoft;

public class WindowsStack : IBluetoothStack
{
    public static WindowsStack Instance { get; } = new();

    public ErrorData StartMonitors(OperationToken token) => Monitors.WindowsMonitors.Start(token);

    public ErrorData StopMonitors() => Monitors.WindowsMonitors.Stop();

    public PlatformInfo GetPlatformInfo() =>
        new()
        {
            Stack = "Microsoft",
            OsInfo = $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})",
        };

    public Features GetFeatureFlags() =>
        new(
            FeatureFlags.FeatureConnection
                | FeatureFlags.FeaturePairing
                | FeatureFlags.FeatureSendFile
                | FeatureFlags.FeatureReceiveFile
        );

    public (ISocket, ErrorData) CreateSocket(BluetoothAddress address, Guid serviceUuid)
    {
        return (new WindowsSocket(address, serviceUuid), Errors.ErrorNone);
    }

    public (ISocketListener, ErrorData) CreateSocketListener(Guid serviceUuid)
    {
        return (new WindowsSocketListener(serviceUuid), Errors.ErrorNone);
    }

    public static async ValueTask<(IAdapterController, ErrorData)> GetDefaultAdapterAsync(
        OperationToken token
    )
    {
        var bluetoothAdapter = await BluetoothAdapter.GetDefaultAsync();

        return bluetoothAdapter == null
            ? (null, Errors.ErrorAdapterNotFound)
            : (new WindowsAdapter(bluetoothAdapter, token), Errors.ErrorNone);
    }

    public (List<AdapterModel>, ErrorData) GetAdapters(OperationToken token)
    {
        var adapters = new List<AdapterModel>();
        var (props, error) = WindowsAdapter.ConvertToAdapterModel(token);
        if (error == Errors.ErrorNone)
        {
            adapters.AddRange(props);
        }

        return (adapters, error);
    }

    public async ValueTask<(IAdapterController, ErrorData)> GetAdapterAsync(
        BluetoothAddress address,
        OperationToken token
    )
    {
        return await GetDefaultAdapterAsync(token);
    }

    public async ValueTask<(IDeviceController, ErrorData)> GetDeviceAsync(
        BluetoothAddress address,
        OperationToken token
    )
    {
        var device = await BluetoothDevice.FromBluetoothAddressAsync(address);
        return device != null
            ? (new WindowsDevice(device, token), Errors.ErrorNone)
            : (null, Errors.ErrorDeviceNotFound);
    }
}
