﻿using GoodTimeStudio.MyPhone.OBEX.Pbap;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.OperationToken;
using InTheHand.Net.Bluetooth;

namespace Haraltd.Stack.Microsoft.Devices.Profiles;

internal static class Pbap
{
    private static bool _clientInProgress;

    internal static async Task<(VcardModel, ErrorData)> GetPhonebookAsync(
        OperationToken token,
        string address,
        string phonebookId
    )
    {
        if (_clientInProgress)
            return (
                new VcardModel(),
                Errors.ErrorOperationInProgress.WrapError(
                    new Dictionary<string, object>
                    {
                        { "operation", "Phonebook Client" },
                        { "exception", "A Phonebook client session is in progress" },
                    }
                )
            );

        try
        {
            _clientInProgress = true;

            using var device = await DeviceUtils.GetBluetoothDeviceWithService(
                address,
                BluetoothService.PhonebookAccessPse
            );
            using BluetoothPbapClientSession pbap = new(device, token.LinkedCancelTokenSource);

            await pbap.ConnectAsync();
            if (pbap.ObexClient == null)
                throw new Exception("PBAP ObexClient is null on connect");

            var phonebook = await pbap.ObexClient.PullPhoneBookAsync(phonebookId);
            return (new VcardModel(phonebookId, phonebook), Errors.ErrorNone);
        }
        catch (Exception e)
        {
            return (
                new VcardModel(),
                Errors.ErrorDevicePhonebookClient.WrapError(
                    new Dictionary<string, object> { { "exception", e.Message } }
                )
            );
        }
        finally
        {
            _clientInProgress = false;
        }
    }

    internal static async Task<(VcardModel, ErrorData)> GetAllContactsAsync(
        OperationToken token,
        string address
    )
    {
        return await GetPhonebookAsync(token, address, "telecom/pb.vcf");
    }

    internal static async Task<(VcardModel, ErrorData)> GetCombinedCallHistoryAsync(
        OperationToken token,
        string address
    )
    {
        return await GetPhonebookAsync(token, address, "telecom/cch.vcf");
    }

    internal static async Task<(VcardModel, ErrorData)> GetIncomingCallsHistoryAsync(
        OperationToken token,
        string address
    )
    {
        return await GetPhonebookAsync(token, address, "telecom/ich.vcf");
    }

    internal static async Task<(VcardModel, ErrorData)> GetOutgoingCallsHistoryAsync(
        OperationToken token,
        string address
    )
    {
        return await GetPhonebookAsync(token, address, "telecom/och.vcf");
    }

    internal static async Task<(VcardModel, ErrorData)> GetMissedCallsAsync(
        OperationToken token,
        string address
    )
    {
        return await GetPhonebookAsync(token, address, "telecom/mch.vcf");
    }

    internal static async Task<(VcardModel, ErrorData)> GetSpeedDialAsync(
        OperationToken token,
        string address
    )
    {
        return await GetPhonebookAsync(token, address, "telecom/spd.vcf");
    }
}
