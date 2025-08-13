using System.Collections.Concurrent;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.Managers;
using Haraltd.Operations.OutputStream;
using Haraltd.Stack.Microsoft.Windows;
using InTheHand.Net;
using Windows.Devices.Enumeration;

namespace Haraltd.Stack.Microsoft.Devices;

internal class Pairing
{
    private static readonly ConcurrentDictionary<BluetoothAddress, OperationToken> PendingPairing =
        new();

    private int _timeout;

    internal async Task<ErrorData> PairAsync(
        OperationToken operationToken,
        string address,
        int timeout = 10
    )
    {
        try
        {
            _timeout = timeout * 1000;

            if (!PendingPairing.TryAdd(BluetoothAddress.Parse(address), operationToken))
                return Errors.ErrorOperationInProgress;

            using var device = await DeviceUtils.GetBluetoothDevice(address);

            if (device.DeviceInformation.Pairing.IsPaired)
                return Errors.ErrorNone;

            OperationManager.SetOperationProperties(operationToken);

            device.DeviceInformation.Pairing.Custom.PairingRequested += PairingRequestEventHandler;
            var pairedStatus = await device.DeviceInformation.Pairing.Custom.PairAsync(
                DevicePairingKinds.DisplayPin
                    | DevicePairingKinds.ConfirmPinMatch
                    | DevicePairingKinds.ConfirmOnly
            );
            device.DeviceInformation.Pairing.Custom.PairingRequested -= PairingRequestEventHandler;

            if (pairedStatus.Status != DevicePairingResultStatus.Paired)
                throw new Exception($"Device pairing status is: {pairedStatus.Status}");
        }
        catch (Exception e)
        {
            return Errors.ErrorDevicePairing.WrapError(
                new Dictionary<string, object>
                {
                    { "device-address", address },
                    { "exception", e.Message },
                }
            );
        }
        finally
        {
            CancelPairing(address);
        }

        return Errors.ErrorNone;
    }

    internal static ErrorData CancelPairing(string address)
    {
        if (PendingPairing.TryRemove(BluetoothAddress.Parse(address), out var token))
            token.Release();
        else
            return Errors.ErrorDeviceNotFound;

        return Errors.ErrorNone;
    }

    private void PairingRequestEventHandler(
        DeviceInformationCustomPairing sender,
        DevicePairingRequestedEventArgs args
    )
    {
        switch (args.PairingKind)
        {
            case DevicePairingKinds.DisplayPin:
            case DevicePairingKinds.ConfirmOnly:
            case DevicePairingKinds.ConfirmPinMatch:
                var deferral = args.GetDeferral();

                DeviceUtils.ParseAepId(args.DeviceInformation.Id, out _, out var deviceAddress);
                if (GetAuthResponse(deviceAddress, args.Pin, args.PairingKind))
                    args.Accept();

                deferral.Complete();
                break;

            default:
                args.Accept();
                break;
        }
    }

    private bool GetAuthResponse(string address, string pin, DevicePairingKinds pairingKinds)
    {
        if (!PendingPairing.TryGetValue(BluetoothAddress.Parse(address), out var token))
            return false;

        WindowsPairingAuthEvent authEvent = new(address, pin, _timeout, pairingKinds, token);

        return Output.ConfirmAuthentication(authEvent);
    }
}
