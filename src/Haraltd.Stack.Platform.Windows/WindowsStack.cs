using System.Runtime.InteropServices;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Stack.Base;
using Haraltd.Stack.Base.Profiles.Opp;
using Haraltd.Stack.Base.Sockets;
using Haraltd.Stack.Obex.SessionManager;
using Haraltd.Stack.Platform.Windows.Adapters;
using Haraltd.Stack.Platform.Windows.Devices;
using Haraltd.Stack.Platform.Windows.Server;
using Haraltd.Stack.Platform.Windows.Sockets;
using InTheHand.Net;
using Windows.Devices.Bluetooth;
using static Haraltd.DataTypes.Models.Features;

namespace Haraltd.Stack.Platform.Windows;

public class WindowsStack : IBluetoothStack
{
    public static IOppSessionManager OppSessionManager { get; } =
        new OppSessionManager(new WindowsStack(), new WindowsServer());

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

    public (ISocket, ErrorData) CreateSocket(SocketOptions options)
    {
        return (new WindowsSocket(options), Errors.ErrorNone);
    }

    public (ISocketListener, ErrorData) CreateSocketListener(SocketListenerOptions options)
    {
        return (new WindowsSocketListener(options), Errors.ErrorNone);
    }

    internal static async ValueTask<(IAdapterController, ErrorData)> GetDefaultAdapterAsync(
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

    public async ValueTask<AdapterControllerResult> GetAdapterAsync(
        BluetoothAddress address,
        OperationToken token
    )
    {
        var (adapter, error) = await GetDefaultAdapterAsync(token);
        return new AdapterControllerResult(adapter, error);
    }

    public async ValueTask<DeviceControllerResult> GetDeviceAsync(
        BluetoothAddress address,
        OperationToken token
    )
    {
        var device = await BluetoothDevice.FromBluetoothAddressAsync(address);
        return device != null
            ? new DeviceControllerResult(new WindowsDevice(device, token), Errors.ErrorNone)
            : new DeviceControllerResult(null, Errors.ErrorDeviceNotFound);
    }
}
