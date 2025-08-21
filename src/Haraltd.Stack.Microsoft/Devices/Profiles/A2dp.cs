﻿using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.Managers;
using Haraltd.Stack.Microsoft.Adapters;
using InTheHand.Net;
using Windows.Devices.Bluetooth;
using Windows.Media.Audio;

namespace Haraltd.Stack.Microsoft.Devices.Profiles;

internal static class A2dp
{
    internal static async Task<ErrorData> StartAudioSessionAsync(
        OperationToken token,
        string address
    )
    {
        try
        {
            AdapterMethods.ThrowIfRadioNotOperable();

            var audioDeviceId = await DeviceUtils.GetDeviceIdBySelector(
                address,
                AudioPlaybackConnection.GetDeviceSelector()
            );

            var audio = AudioPlaybackConnection.TryCreateFromId(audioDeviceId);
            if (audio.State == AudioPlaybackConnectionState.Opened)
                return Errors.ErrorDeviceAlreadyConnected;

            await audio.StartAsync();

            var openResult = await audio.OpenAsync();
            if (openResult.Status != AudioPlaybackConnectionOpenResultStatus.Success)
                return Errors.ErrorDeviceA2dpClient.AddMetadata(
                    "audio-connection",
                    $"{openResult.Status}"
                );

            OperationManager.SetOperationProperties(token);

            _ = Task.Run(async () =>
            {
                using (audio)
                {
                    try
                    {
                        using var windowsDevice = await BluetoothDevice.FromBluetoothAddressAsync(
                            BluetoothAddress.Parse(address)
                        );
                        windowsDevice.ConnectionStatusChanged += (s, _) =>
                        {
                            if (s.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
                                token.Release();
                        };
                        if (
                            windowsDevice.ConnectionStatus == BluetoothConnectionStatus.Disconnected
                        )
                            return;

                        token.Wait();
                    }
                    finally
                    {
                        token.Release();
                    }
                }
            });
        }
        catch (Exception e)
        {
            return Errors.ErrorDeviceA2dpClient.WrapError(
                new Dictionary<string, object> { { "exception", e.Message } }
            );
        }

        return Errors.ErrorNone;
    }
}
