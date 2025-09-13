using System.Collections.Concurrent;
using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.Managers;
using Haraltd.Operations.OutputStream;
using Haraltd.Stack.Platform.MacOS.MacOS;
using IOBluetooth;
using IOBluetooth.Shim;

namespace Haraltd.Stack.Platform.MacOS.Devices;

internal static class Pairing
{
    private static readonly ConcurrentDictionary<string, OperationToken> PairingOperations = new();

    internal static async Task<ErrorData> StartAsync(
        OperationToken token,
        BluetoothDevice device,
        int timeout = 10
    )
    {
        if (PairingOperations.ContainsKey(device.AddressString))
            return Errors.ErrorDevicePairing.AddMetadata(
                "exception",
                "The device is already being paired"
            );

        try
        {
            using var pairingDelegate = new PairingDelegate(token, timeout);
            using var pairingController = new DevicePair(pairingDelegate);

            BluetoothShim.SetNilPeripheral(device);

            pairingController.Device = device;
            var res = pairingController.Start();
            if (res != 0)
                return Errors.ErrorDevicePairing.AddMetadata(
                    "exception",
                    "The device pairing cannot be started"
                );

            PairingOperations.TryAdd(device.AddressString, token);

            OperationManager.SetOperationProperties(token);
            res = await pairingDelegate.WaitForDevicePair();
            if (res != 0)
                return Errors.ErrorDevicePairing;
        }
        finally
        {
            token.Release();
            PairingOperations.TryRemove(device.AddressString, out _);
        }

        return Errors.ErrorNone;
    }

    internal static ErrorData Stop(OperationToken token, BluetoothDevice device)
    {
        if (!PairingOperations.TryGetValue(device.AddressString, out var clientToken))
            return Errors.ErrorUnexpected.AddMetadata(
                "exception",
                "The device is not being paired"
            );

        clientToken.Release();

        return Errors.ErrorNone;
    }
}

internal class PairingDelegate : DevicePairDelegate
{
    private readonly TaskCompletionSource<int> _taskCompletionSource = new();
    private readonly OperationToken _token;
    private readonly int _timeout;

    internal PairingDelegate(OperationToken token, int timeout)
    {
        _token = token;
        _timeout = timeout * 1000;
    }

    internal async Task<int> WaitForDevicePair()
    {
        return await _taskCompletionSource.Task;
    }

    public override void PairingFinished(DevicePair sender, int error)
    {
        _taskCompletionSource.TrySetResult(error);
    }

    public override void PairingPinCodeRequest(DevicePair sender)
    {
        var pincode = (uint)Random.Shared.Next(1000, 9999);
        var pincodeText = pincode.ToString();

        var bluetoothPin = pincode.ToNativePinCode(out var length);

        var authEvent = new MacOsPairingAuthEvent(
            sender.Device.FormattedAddress!,
            pincodeText,
            _timeout,
            AuthenticationEvent.AuthenticationEventType.ConfirmPasskey,
            _token
        );

        var auth = Output.ConfirmAuthentication(authEvent);
        try
        {
            if (auth)
                sender.ReplyPinCode(length, ref bluetoothPin);
        }
        catch
        {
            _taskCompletionSource.TrySetResult(1);
        }
    }

    public override void PairingUserConfirmationRequest(DevicePair sender, uint numericValue)
    {
        var pincodeText = numericValue.ToString();
        var bluetoothPin = numericValue.ToNativePinCode(out var length);

        var authEvent = new MacOsPairingAuthEvent(
            sender.Device.FormattedAddress!,
            pincodeText,
            _timeout,
            AuthenticationEvent.AuthenticationEventType.ConfirmPasskey,
            _token
        );

        var auth = Output.ConfirmAuthentication(authEvent);
        try
        {
            if (auth)
                sender.ReplyPinCode(length, ref bluetoothPin);
        }
        catch
        {
            _taskCompletionSource.TrySetResult(1);
        }
    }

    public override void PairingUserPasskeyNotification(DevicePair sender, uint passkey)
    {
        var authEvent = new MacOsPairingAuthEvent(
            sender.Device.FormattedAddress!,
            passkey.ToString(),
            _timeout,
            AuthenticationEvent.AuthenticationEventType.DisplayPasskey,
            _token
        );

        Output.ConfirmAuthentication(authEvent);
    }

    public override void SimplePairingComplete(DevicePair sender, byte status) { }
}
