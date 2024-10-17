using Bluetuith.Shim.Executor;
using Bluetuith.Shim.Executor.OutputHandler;
using Bluetuith.Shim.Stack.Events;
using Bluetuith.Shim.Types;
using InTheHand.Net;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Foundation;

namespace Bluetuith.Shim.Stack.Providers.MSFT.Devices;

internal sealed class Pairing
{
    private OperationToken _operationToken;
    private int _timeout;

    public Pairing(OperationToken operationToken)
    {
        _operationToken = operationToken;
    }

    public async Task<ErrorData> PairAsync(string address, int timeout = 10)
    {
        try
        {
            _timeout = timeout * 1000;

            using BluetoothDevice device = await BluetoothDevice.FromBluetoothAddressAsync(BluetoothAddress.Parse(address));
            if (device == null)
            {
                throw new Exception(StackErrors.ErrorDeviceNotFound.Description);
            }

            if (device.DeviceInformation.Pairing.IsPaired)
            {
                return Errors.ErrorNone;
            }

            device.DeviceInformation.Pairing.Custom.PairingRequested += PairingRequestEventHandler;
            DevicePairingResult pairedStatus = await device.DeviceInformation.Pairing.Custom.PairAsync(
                DevicePairingKinds.DisplayPin | DevicePairingKinds.ConfirmPinMatch | DevicePairingKinds.ConfirmOnly
            );
            device.DeviceInformation.Pairing.Custom.PairingRequested -= PairingRequestEventHandler;

            if (pairedStatus.Status != DevicePairingResultStatus.Paired)
            {
                throw new Exception($"Device pairing status is: {pairedStatus.Status}");
            }
        }
        catch (Exception e)
        {
            return StackErrors.ErrorDevicePairing.WrapError(new() {
                {"device-address", address },
                {"exception",  e.Message}
            });
        }
        finally
        {
            _operationToken.CancelTokenSource.Cancel();
        }

        return Errors.ErrorNone;
    }

    private void PairingRequestEventHandler(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
    {
        switch (args.PairingKind)
        {
            case DevicePairingKinds.DisplayPin:
            case DevicePairingKinds.ConfirmOnly:
                args.Accept();
                break;

            case DevicePairingKinds.ConfirmPinMatch:
                Deferral deferral = args.GetDeferral();

                if (GetAuthResponse(args.Pin))
                {
                    args.Accept();
                }

                deferral.Complete();
                break;
        }
    }

    private bool GetAuthResponse(string pin)
    {
        PairingAuthenticationEvent authEvent = new(
            pin,
            _timeout,
            _operationToken.CancelTokenSource
        );

        return Output.ConfirmAuthentication(authEvent, _operationToken);
    }
}
