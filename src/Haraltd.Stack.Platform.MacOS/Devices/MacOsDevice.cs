using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Stack.Base;
using Haraltd.Stack.Base.Profiles.Opp;
using InTheHand.Net;
using IOBluetooth;
using IOBluetooth.Shim;

namespace Haraltd.Stack.Platform.MacOS.Devices;

internal class MacOsDevice(BluetoothDevice device, OperationToken token) : IDeviceController
{
    public BluetoothAddress Address => device.FormattedAddress ?? BluetoothAddress.None;

    public (DeviceModel, ErrorData) Properties()
    {
        return device.ToDeviceModel();
    }

    public async ValueTask<ErrorData> ConnectAsync()
    {
        return await ConnectProfileAsync(Guid.Empty);
    }

    public async ValueTask<ErrorData> ConnectProfileAsync(Guid profileGuid)
    {
        if (CheckError(false, true, out var error))
            return error;

        try
        {
            var connected = false;

            if (profileGuid == Guid.Empty)
            {
                var asyncCallBack = new ConnectionAsyncCallBack();
                device.OpenConnection(asyncCallBack);
                connected = await asyncCallBack.GetConnectionStatus();
            }
            else
            {
                return Errors.ErrorUnsupported;
            }

            return connected ? Errors.ErrorNone : Errors.ErrorDeviceNotConnected;
        }
        catch (Exception ex)
        {
            return Errors.ErrorDeviceNotConnected.AddMetadata("exception", ex.Message);
        }
    }

    public async ValueTask<ErrorData> DisconnectAsync()
    {
        await ValueTask.CompletedTask;

        if (CheckError(false, false, out var error))
            return error;

        try
        {
            var result = device.CloseConnection();

            token.ReleaseAfter(2000);
            while (!token.IsReleased())
            {
                if (!device.IsConnected)
                    break;
            }

            if (device.IsConnected)
                return Errors.ErrorDeviceDisconnect;

            return result != 0 ? Errors.ErrorDeviceDisconnect : Errors.ErrorNone;
        }
        catch (Exception ex)
        {
            return Errors.ErrorUnexpected.AddMetadata("exception", ex.Message);
        }
    }

    public async ValueTask<ErrorData> DisconnectProfileAsync(Guid profileGuid)
    {
        return await DisconnectAsync();
    }

    public async Task<ErrorData> PairAsync(int timeout = 10)
    {
        if (CheckError(true, null, out var error))
            return error;

        return await Pairing.StartAsync(token, device, timeout);
    }

    public ErrorData CancelPairing()
    {
        return Pairing.Stop(token, device);
    }

    public async ValueTask<ErrorData> StartAudioSessionAsync()
    {
        return await ValueTask.FromResult(Errors.ErrorUnsupported);
    }

    public DeviceOppManager GetOppManager()
    {
        return new DeviceOppManager(MacOsStack.OppSessionManager, token, Address);
    }

    public async ValueTask<ErrorData> RemoveAsync()
    {
        if (CheckError(false, null, out var error))
            return error;

        try
        {
            BluetoothShim.RemoveKnownDevice(device);
        }
        catch (Exception ex)
        {
            return Errors.ErrorDeviceUnpairing.AddMetadata("exception", ex.Message);
        }

        return await ValueTask.FromResult(Errors.ErrorNone);
    }

    private bool CheckError(bool? pairedState, bool? connectedState, out ErrorData error)
    {
        error = Errors.ErrorNone;

        if (HostController.DefaultController.PowerState != HciPowerState.On)
        {
            error = Errors.ErrorAdapterStateAccess.AddMetadata(
                "exception",
                "Adapter is not powered on"
            );
            return false;
        }

        if (error == Errors.ErrorNone && pairedState != null && device.IsPaired == pairedState)
        {
            error = pairedState switch
            {
                true => Errors.ErrorDeviceAlreadyPaired,
                false => Errors.ErrorDeviceNotPaired,
                _ => error,
            };
        }

        if (
            error == Errors.ErrorNone
            && connectedState != null
            && device.IsConnected == connectedState
        )
        {
            error = connectedState switch
            {
                true => Errors.ErrorDeviceAlreadyConnected,
                false => Errors.ErrorDeviceNotConnected,
                _ => error,
            };
        }

        return error != Errors.ErrorNone;
    }

    public void Dispose()
    {
        device.Dispose();
    }
}

internal class ConnectionAsyncCallBack : BluetoothDeviceAsyncCallbacks
{
    private readonly TaskCompletionSource<bool> _taskCompletionSource = new();

    public override void ConnectionComplete(BluetoothDevice device, int status)
    {
        _taskCompletionSource.TrySetResult(status == 0);
    }

    public async Task<bool> GetConnectionStatus()
    {
        await Task.WhenAny(_taskCompletionSource.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        return _taskCompletionSource.Task.IsCompleted && _taskCompletionSource.Task.Result;
    }
}
