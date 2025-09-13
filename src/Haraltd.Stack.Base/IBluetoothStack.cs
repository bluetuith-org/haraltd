using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Stack.Base.Sockets;
using InTheHand.Net;

namespace Haraltd.Stack.Base;

public readonly struct AdapterControllerResult(IAdapterController adapter, ErrorData error)
    : IDisposable
{
    public IAdapterController Adapter => adapter;

    public ErrorData Error => error;

    public void Dispose()
    {
        Adapter?.Dispose();
    }
}

public readonly struct DeviceControllerResult(IDeviceController device, ErrorData error)
    : IDisposable
{
    public IDeviceController Device => device;
    public ErrorData Error => error;

    public void Dispose()
    {
        device?.Dispose();
    }
}

public interface IBluetoothStack
{
    public ErrorData StartMonitors(OperationToken token);
    public ErrorData StopMonitors();

    public Features GetFeatureFlags();
    public PlatformInfo GetPlatformInfo();

    public (ISocket, ErrorData) CreateSocket(SocketOptions options);
    public (ISocketListener, ErrorData) CreateSocketListener(SocketListenerOptions options);

    public (List<AdapterModel>, ErrorData) GetAdapters(OperationToken token);

    public ValueTask<AdapterControllerResult> GetAdapterAsync(
        BluetoothAddress address,
        OperationToken token
    );
    public async ValueTask<AdapterControllerResult> GetAdapterAsync(
        string address,
        OperationToken token
    ) =>
        GetBluetoothAddress(address, out var addr, out var error)
            ? await GetAdapterAsync(addr, token)
            : new AdapterControllerResult(null, error);

    public ValueTask<DeviceControllerResult> GetDeviceAsync(
        BluetoothAddress address,
        OperationToken token
    );

    public async ValueTask<DeviceControllerResult> GetDeviceAsync(
        string address,
        OperationToken token
    ) =>
        GetBluetoothAddress(address, out var addr, out var error)
            ? await GetDeviceAsync(addr, token)
            : new DeviceControllerResult(null, error);

    public bool GetBluetoothAddress(string address, out BluetoothAddress addr, out ErrorData error)
    {
        error = null;

        var parsed = BluetoothAddress.TryParse(address, out addr);
        if (!parsed)
            error = Errors.ErrorUnexpected.AddMetadata("exception", "Address is not valid");

        return parsed;
    }
}
