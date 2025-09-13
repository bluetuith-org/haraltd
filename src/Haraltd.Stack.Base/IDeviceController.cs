using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.Stack.Base.Profiles.Opp;
using InTheHand.Net;

namespace Haraltd.Stack.Base;

public interface IDeviceController : IDisposable
{
    public BluetoothAddress Address { get; }

    public (DeviceModel, ErrorData) Properties();

    public ValueTask<ErrorData> ConnectAsync();
    public ValueTask<ErrorData> ConnectProfileAsync(Guid profileGuid);

    public ValueTask<ErrorData> DisconnectAsync();
    public ValueTask<ErrorData> DisconnectProfileAsync(Guid profileGuid);

    public Task<ErrorData> PairAsync(int timeout = 10);
    public ErrorData CancelPairing();

    public ValueTask<ErrorData> StartAudioSessionAsync();

    public DeviceOppManager GetOppManager();

    public ValueTask<ErrorData> RemoveAsync();
}
