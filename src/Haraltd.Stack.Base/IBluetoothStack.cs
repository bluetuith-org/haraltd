using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Stack.Base.Sockets;
using InTheHand.Net;

namespace Haraltd.Stack.Base;

public interface IBluetoothStack
{
    public ErrorData StartMonitors(OperationToken token);
    public ErrorData StopMonitors();

    public Features GetFeatureFlags();
    public PlatformInfo GetPlatformInfo();

    public (ISocket, ErrorData) CreateSocket(BluetoothAddress address, Guid serviceUuid);
    public (ISocketListener, ErrorData) CreateSocketListener(Guid serviceUuid);

    public (List<AdapterModel>, ErrorData) GetAdapters(OperationToken token);

    public ValueTask<(IAdapterController, ErrorData)> GetAdapterAsync(
        BluetoothAddress address,
        OperationToken token
    );
    public async ValueTask<(IAdapterController, ErrorData)> GetAdapterAsync(
        string address,
        OperationToken token
    ) =>
        GetBluetoothAddress(address, out var addr, out var error)
            ? await GetAdapterAsync(addr, token)
            : (null, error);

    public ValueTask<(IDeviceController, ErrorData)> GetDeviceAsync(
        BluetoothAddress address,
        OperationToken token
    );
    public async ValueTask<(IDeviceController, ErrorData)> GetDeviceAsync(
        string address,
        OperationToken token
    ) =>
        GetBluetoothAddress(address, out var addr, out var error)
            ? await GetDeviceAsync(addr, token)
            : (null, error);

    public bool GetBluetoothAddress(string address, out BluetoothAddress addr, out ErrorData error)
    {
        addr = null;
        error = Errors.ErrorNone;

        try
        {
            addr = BluetoothAddress.Parse(address);
        }
        catch (Exception ex)
        {
            error = Errors.ErrorUnexpected.AddMetadata("exception", ex.Message);
        }

        return error == Errors.ErrorNone;
    }
}
