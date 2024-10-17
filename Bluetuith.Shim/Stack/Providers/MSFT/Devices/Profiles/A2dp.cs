using Bluetuith.Shim.Executor;
using Bluetuith.Shim.Executor.OutputHandler;
using Bluetuith.Shim.Stack.Events;
using Bluetuith.Shim.Types;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;

namespace Bluetuith.Shim.Stack.Providers.MSFT.Devices.Profiles;

internal sealed class A2dp
{
    private static bool _clientInProgress = false;

    public static async Task<ErrorData> StartAudioSessionAsync(OperationToken token, string address)
    {
        if (_clientInProgress)
        {
            return Errors.ErrorOperationInProgress.WrapError(new()
                {
                    {"operation", "A2dp Client" },
                    {"exception", $"An Advanced Audio Distribution Profile client session is in progress" }
                });
        }

        var deviceFound = false;
        DeviceConnectionStatusEvent deviceConnectionEvent = new(address, StackEventCode.A2dpClientEventCode);

        try
        {
            _clientInProgress = true;

            var audioDeviceId = await DeviceUtils.GetDeviceIdBySelector(address, AudioPlaybackConnection.GetDeviceSelector());

            using var audio = AudioPlaybackConnection.TryCreateFromId(audioDeviceId);
            deviceFound = true;

            Output.Event(
                deviceConnectionEvent with { State = ConnectionStatusEvent.ConnectionStatus.DeviceConnecting },
                token
            );

            await audio.StartAsync();

            AudioPlaybackConnectionOpenResult openResult = await audio.OpenAsync();
            if (openResult.Status != AudioPlaybackConnectionOpenResultStatus.Success)
            {
                return StackErrors.ErrorDeviceA2dpClient.WrapError(new() {
                        {"exception", $"Could not open an Advanced Audio Distribution Profile client session for device {address}: {openResult.Status}" }
                    });
            }
            Output.Event(
                deviceConnectionEvent with { State = ConnectionStatusEvent.ConnectionStatus.DeviceConnected },
                token
            );

            DeviceWatcher deviceWatcher = DeviceInformation.CreateWatcher(AudioPlaybackConnection.GetDeviceSelector());
            deviceWatcher.Removed += (sender, infoUpdate) =>
            {
                if (audioDeviceId == infoUpdate.Id)
                {
                    token.CancelTokenSource.Cancel();
                }
            };
            deviceWatcher.Start();

            token.CancelToken.WaitHandle.WaitOne();

            deviceWatcher.Stop();
        }
        catch (Exception e)
        {
            return StackErrors.ErrorDeviceA2dpClient.WrapError(new()
            {
                {"exception", e.Message}
            });
        }
        finally
        {
            if (deviceFound)
            {
                Output.Event(
                    deviceConnectionEvent with { State = ConnectionStatusEvent.ConnectionStatus.DeviceDisconnected },
                    token
                );
            }

            _clientInProgress = false;
        }

        return Errors.ErrorNone;
    }
}
