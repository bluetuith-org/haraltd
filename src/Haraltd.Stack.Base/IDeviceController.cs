using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using InTheHand.Net;

namespace Haraltd.Stack.Base;

public interface IDeviceController : IDisposable
{
    public BluetoothAddress Address { get; }

    public event Action<bool> OnDeviceConnectionStateChanged;

    public (DeviceModel, ErrorData) Properties();

    public ValueTask<ErrorData> ConnectAsync();
    public ValueTask<ErrorData> ConnectProfileAsync(Guid profileGuid);

    public ValueTask<ErrorData> DisconnectAsync();
    public ValueTask<ErrorData> DisconnectProfileAsync(Guid profileGuid);

    public Task<ErrorData> PairAsync(int timeout = 10);
    public ErrorData CancelPairing();

    public ValueTask<ErrorData> StartAudioSessionAsync();

    public ValueTask<ErrorData> StartFileTransferSessionAsync();

    public (FileTransferModel, ErrorData) QueueFileSend(string filePath);

    public ValueTask<ErrorData> CancelFileTransferSessionAsync();

    public ValueTask<ErrorData> StopFileTransferSessionAsync();

    public ValueTask<ErrorData> RemoveAsync();
}
