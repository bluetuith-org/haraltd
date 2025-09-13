using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.Stack.Base.Profiles.Opp;
using InTheHand.Net;

namespace Haraltd.Stack.Base;

public interface IAdapterController : IDisposable
{
    public BluetoothAddress Address { get; }

    public (AdapterModel, ErrorData) Properties();

    public ErrorData SetPoweredState(bool enable);
    public ErrorData SetPairableState(bool enable);
    public ErrorData SetDiscoverableState(bool enable);

    public (GenericResult<List<DeviceModel>>, ErrorData) GetPairedDevices();
    public Guid[] GetServices();

    public ErrorData StartDeviceDiscovery(int timeout = 0);
    public ErrorData StopDeviceDiscovery();

    public AdapterOppManager GetOppManager();

    public ValueTask<ErrorData> DisconnectDeviceAsync(BluetoothAddress address);
    public Task<ErrorData> RemoveDeviceAsync(BluetoothAddress address);
}
