using System.Runtime.InteropServices;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Stack.Base;
using Haraltd.Stack.Base.Profiles.Opp;
using Haraltd.Stack.Base.Sockets;
using Haraltd.Stack.Obex.SessionManager;
using Haraltd.Stack.Platform.MacOS.Adapters;
using Haraltd.Stack.Platform.MacOS.Devices;
using Haraltd.Stack.Platform.MacOS.Monitors;
using Haraltd.Stack.Platform.MacOS.Server;
using Haraltd.Stack.Platform.MacOS.Sockets;
using InTheHand.Net;
using IOBluetooth;
using IOBluetooth.Shim;

namespace Haraltd.Stack.Platform.MacOS;

public class MacOsStack : IBluetoothStack
{
    public static IOppSessionManager OppSessionManager { get; } =
        new OppSessionManager(new MacOsStack(), new MacOsServer());

    public ErrorData StartMonitors(OperationToken token) => MacOsMonitors.Start(token);

    public ErrorData StopMonitors() => MacOsMonitors.Stop();

    public PlatformInfo GetPlatformInfo() =>
        new()
        {
            Stack = "MacOS",
            OsInfo = $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})",
        };

    public Features GetFeatureFlags() =>
        new(
            Features.FeatureFlags.FeatureConnection
                | Features.FeatureFlags.FeaturePairing
                | Features.FeatureFlags.FeatureSendFile
                | Features.FeatureFlags.FeatureReceiveFile
        );

    public (ISocket, ErrorData) CreateSocket(SocketOptions options)
    {
        if (
            HostController.DefaultController == null
            || HostController.DefaultController.PowerState != HciPowerState.On
        )
            return (null, Errors.ErrorAdapterNotFound)!;

        return (new MacOsSocket(options), Errors.ErrorNone);
    }

    public (ISocketListener, ErrorData) CreateSocketListener(SocketListenerOptions options)
    {
        if (
            HostController.DefaultController == null
            || HostController.DefaultController.PowerState != HciPowerState.On
        )
            return (null, Errors.ErrorAdapterNotFound)!;

        return (new MacOsSocketListener(options), Errors.ErrorNone);
    }

    public (List<AdapterModel>, ErrorData) GetAdapters(OperationToken token)
    {
        var adapter = HostController.DefaultController;
        if (adapter == null)
            return ([], Errors.ErrorAdapterNotFound);

        var (adapterModel, error) = adapter.ToAdapterModel(
            AdapterNativeMethods.GetActualHostControllerAddress(adapter)
        );

        return ([adapterModel], error);
    }

    public async ValueTask<AdapterControllerResult> GetAdapterAsync(
        BluetoothAddress address,
        OperationToken token
    )
    {
        await ValueTask.CompletedTask;

        var adapter = HostController.DefaultController;
        if (adapter == null || adapter.AddressAsString != address.ToString("C"))
            return new AdapterControllerResult(null!, Errors.ErrorAdapterNotFound);

        return new AdapterControllerResult(new MacOsAdapter(adapter, token), Errors.ErrorNone);
    }

    public async ValueTask<DeviceControllerResult> GetDeviceAsync(
        BluetoothAddress address,
        OperationToken token
    )
    {
        await ValueTask.CompletedTask;

        if (HostController.DefaultController == null)
        {
            return new DeviceControllerResult(null!, Errors.ErrorAdapterNotFound);
        }

        var isKnownDevice = false;
        var device = BluetoothShim.GetKnownDevice(address.ToString("C"), ref isKnownDevice);

        return !isKnownDevice
            ? new DeviceControllerResult(null, Errors.ErrorDeviceNotFound)
            : new DeviceControllerResult(new MacOsDevice(device, token), Errors.ErrorNone);
    }
}
