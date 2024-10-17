using Bluetuith.Shim.Executor;
using Bluetuith.Shim.Executor.OutputHandler;
using Bluetuith.Shim.Stack.Events;
using Bluetuith.Shim.Stack.Models;
using Bluetuith.Shim.Types;
using GoodTimeStudio.MyPhone.OBEX.Pbap;
using InTheHand.Net.Bluetooth;
using Windows.Devices.Bluetooth;

namespace Bluetuith.Shim.Stack.Providers.MSFT.Devices.Profiles;

internal sealed class Pbap
{
    private static bool _clientInProgress = false;

    public static async Task<(VcardModel, ErrorData)> GetPhonebookAsync(OperationToken token, string address, string phonebookId)
    {
        if (_clientInProgress)
        {
            return (new(), Errors.ErrorOperationInProgress.WrapError(new() {
                {"operation", "Phonebook Client" },
                {"exception", "A Phonebook client session is in progress"}
            }));
        }

        var deviceFound = false;
        DeviceConnectionStatusEvent deviceConnectionEvent = new(address, StackEventCode.PhonebookAccessClientEventCode);

        try
        {
            _clientInProgress = true;

            using BluetoothDevice device = await DeviceUtils.GetBluetoothDeviceWithService(address, BluetoothService.PhonebookAccessPse);
            using BluetoothPbapClientSession pbap = new(device, token.CancelTokenSource);
            deviceFound = true;

            Output.Event(
                deviceConnectionEvent with { State = ConnectionStatusEvent.ConnectionStatus.DeviceConnecting },
                token
            );

            await pbap.ConnectAsync();
            if (pbap.ObexClient == null)
            {
                throw new Exception("PBAP ObexClient is null on connect");
            }

            Output.Event(
                deviceConnectionEvent with { State = ConnectionStatusEvent.ConnectionStatus.DeviceConnected },
                token
            );

            var phonebook = await pbap.ObexClient.PullPhoneBookAsync(phonebookId);
            return (new VcardModel(phonebookId, phonebook), Errors.ErrorNone);
        }
        catch (Exception e)
        {
            return (new(), StackErrors.ErrorDevicePhonebookClient.WrapError(new()
            {
                {"exception", e.Message}
            }));
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
    }

    public static async Task<(VcardModel, ErrorData)> GetAllContactsAsync(OperationToken token, string address)
    {
        return await GetPhonebookAsync(token, address, "telecom/pb.vcf");
    }

    public static async Task<(VcardModel, ErrorData)> GetCombinedCallHistoryAsync(OperationToken token, string address)
    {
        return await GetPhonebookAsync(token, address, "telecom/cch.vcf");
    }

    public static async Task<(VcardModel, ErrorData)> GetIncomingCallsHistoryAsync(OperationToken token, string address)
    {
        return await GetPhonebookAsync(token, address, "telecom/ich.vcf");
    }

    public static async Task<(VcardModel, ErrorData)> GetOutgoingCallsHistoryAsync(OperationToken token, string address)
    {
        return await GetPhonebookAsync(token, address, "telecom/och.vcf");
    }

    public static async Task<(VcardModel, ErrorData)> GetMissedCallsAsync(OperationToken token, string address)
    {
        return await GetPhonebookAsync(token, address, "telecom/mch.vcf");
    }

    public static async Task<(VcardModel, ErrorData)> GetSpeedDialAsync(OperationToken token, string address)
    {
        return await GetPhonebookAsync(token, address, "telecom/spd.vcf");
    }
}
