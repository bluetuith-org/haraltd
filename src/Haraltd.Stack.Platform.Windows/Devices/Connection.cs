using DotNext.Collections.Generic;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Stack.Platform.Windows.Adapters;
using Haraltd.Stack.Platform.Windows.Devices.ConnectionMethods;
using Haraltd.Stack.Platform.Windows.Profiles;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using Windows.Devices.Bluetooth;
using Windows.Foundation;

namespace Haraltd.Stack.Platform.Windows.Devices;

internal static class Connection
{
    private static bool _operationInProgress;

    private static readonly Guid[] AppSupportedProfiles =
    [
        BluetoothService.AudioSource,
        BluetoothService.AudioSink,
        BluetoothService.Handsfree,
        BluetoothService.Headset,
    ];

    internal static async ValueTask<ErrorData> ConnectAsync(
        OperationToken token,
        BluetoothAddress address
    )
    {
        if (_operationInProgress)
            return Errors.ErrorOperationInProgress;

        _operationInProgress = true;

        try
        {
            WindowsAdapter.ThrowIfRadioNotOperable();

            if (
                !IsAdapterSupported(token, out var error, AppSupportedProfiles)
                || !IsDeviceSupported(
                    out var device,
                    out var supportedProfiles,
                    out error,
                    token,
                    address,
                    AppSupportedProfiles
                )
            )
                return error;

            return await TryConnectionMethods(token, address, device, supportedProfiles);
        }
        catch (Exception e)
        {
            return Errors.ErrorDeviceNotConnected.AddMetadata("exception", e.Message);
        }
        finally
        {
            _operationInProgress = false;
        }
    }

    internal static async ValueTask<ErrorData> ConnectProfileAsync(
        OperationToken token,
        BluetoothAddress address,
        Guid profileGuid
    )
    {
        if (_operationInProgress)
            return Errors.ErrorOperationInProgress;

        _operationInProgress = true;

        try
        {
            WindowsAdapter.ThrowIfRadioNotOperable();

            if (
                !IsAdapterSupported(token, out var error, profileGuid)
                || !IsDeviceSupported(out var device, out _, out error, token, address, profileGuid)
            )
                return error;

            return await TryConnectionMethods(token, address, device, profileGuid);
        }
        catch (Exception e)
        {
            return Errors.ErrorDeviceNotConnected.AddMetadata("exception", e.Message);
        }
        finally
        {
            _operationInProgress = false;
        }
    }

    private static async ValueTask<ErrorData> TryConnectionMethods(
        OperationToken token,
        BluetoothAddress address,
        DeviceModel device,
        params Guid[] supportedProfiles
    )
    {
        using var windowsDevice = await BluetoothDevice.FromBluetoothAddressAsync(address);

        if (windowsDevice == null)
            return Errors.ErrorDeviceNotFound;

        var error = Errors.ErrorDeviceNotConnected;

        var connectionDone = new CancellationTokenSource();

        TypedEventHandler<BluetoothDevice, object> connectionStatusChanged = (_, _) =>
            connectionDone.Cancel();

        try
        {
            if (!windowsDevice.DeviceInformation.Pairing.IsPaired)
            {
                error = new Pairing().PairAsync(token, address).Result;
                if (!CheckError(error))
                    goto FinishConnection;
            }

            // TODO: Handle case where "Device setup is in progress" and the device is connected.
            windowsDevice.ConnectionStatusChanged += connectionStatusChanged;

            if (
                supportedProfiles.Contains(AppSupportedProfiles[0])
                && A2dp.StartAudioSessionAsync(token, address).Result is var a2dpError
            )
                if (CheckError(a2dpError))
                    goto FinishConnection;

            if (
                supportedProfiles.Length > 0
                && AudioEndpoints.Connect(device, windowsDevice.DeviceInformation) is var audioError
            )
                if (CheckError(audioError))
                    goto FinishConnection;

            if (PnPDeviceStates.Toggle(device) is var toggleError)
                CheckError(toggleError);

            FinishConnection:
            if (error != Errors.ErrorNone)
                return error;

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                connectionDone.Token,
                ConnectionSettings.TimeoutCancelToken.Token
            );
            linkedCts.Token.WaitHandle.WaitOne();

            if (windowsDevice.ConnectionStatus != BluetoothConnectionStatus.Connected)
                error = Errors.ErrorDeviceNotConnected;
        }
        finally
        {
            windowsDevice.ConnectionStatusChanged -= connectionStatusChanged;
        }

        return error;

        bool CheckError(ErrorData e)
        {
            if (error != Errors.ErrorNone)
                error = e;

            return e == Errors.ErrorNone;
        }
    }

    private static bool IsDeviceSupported(
        out DeviceModel device,
        out Guid[] supportedProfiles,
        out ErrorData errors,
        in OperationToken token,
        in BluetoothAddress address,
        params Guid[] profiles
    )
    {
        errors = Errors.ErrorNone;
        supportedProfiles = null;
        device = null;

        if (token.CancelTokenSource.IsCancellationRequested)
        {
            errors = Errors.ErrorOperationCancelled;
            goto FinishChecks;
        }

        (device, errors) = WindowsDevice.ConvertToDeviceModel(address);
        if (errors != Errors.ErrorNone)
            goto FinishChecks;

        if (device.OptionConnected == null || device.OptionUuiDs == null)
        {
            errors = Errors.ErrorUnexpected.AddMetadata(
                "exception",
                "The device is not connected or its services are not found"
            );
            goto FinishChecks;
        }

        var connected = device.OptionConnected == true;
        var uuids = device.OptionUuiDs;

        if (connected)
        {
            errors = Errors.ErrorDeviceAlreadyConnected;
            goto FinishChecks;
        }

        supportedProfiles = [.. uuids.ToList().Intersect(profiles)];
        if (supportedProfiles.Length == 0)
            errors = Errors.ErrorAdapterServicesNotSupported.AddMetadata(
                "profiles",
                profiles.ToString(", ")
            );

        FinishChecks:
        return errors == Errors.ErrorNone;
    }

    private static bool IsAdapterSupported(
        in OperationToken token,
        out ErrorData errors,
        params Guid[] profiles
    )
    {
        WindowsAdapter.ThrowIfRadioNotOperable();

        errors = Errors.ErrorNone;
        if (token.CancelTokenSource.IsCancellationRequested)
        {
            errors = Errors.ErrorOperationCancelled;
            goto FinishChecks;
        }

        var (adapter, convertError) = WindowsAdapter.ConvertToAdapterModel(token);
        if (convertError != Errors.ErrorNone)
        {
            errors = convertError;
            goto FinishChecks;
        }

        var supportedProfiles = adapter.UuiDs.ToList().Intersect(profiles);
        if (!supportedProfiles.Any())
            errors = Errors.ErrorAdapterServicesNotSupported.AddMetadata(
                "profiles",
                profiles.ToString(", ")
            );

        FinishChecks:
        return errors == Errors.ErrorNone;
    }
}
