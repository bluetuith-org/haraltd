using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.Managers;
using Haraltd.Stack.Platform.Windows.Adapters;
using Haraltd.Stack.Platform.Windows.Devices;
using InTheHand.Net;
using Windows.Devices.Bluetooth;
using Windows.Media.Audio;

namespace Haraltd.Stack.Platform.Windows.Profiles;

internal static class A2dp
{
    internal static async ValueTask<ErrorData> StartAudioSessionAsync(
        OperationToken token,
        BluetoothAddress address
    )
    {
        try
        {
            WindowsAdapter.ThrowIfRadioNotOperable();

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
                            address
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
