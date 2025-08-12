using Bluetuith.Shim.DataTypes.Generic;
using Bluetuith.Shim.DataTypes.Models;
using Bluetuith.Shim.DataTypes.OperationToken;
using Bluetuith.Shim.Operations.OutputStream;
using Bluetuith.Shim.Stack.Microsoft.Windows;
using GoodTimeStudio.MyPhone.OBEX;
using GoodTimeStudio.MyPhone.OBEX.Map;
using InTheHand.Net.Bluetooth;
using MixERP.Net.VCards;

namespace Bluetuith.Shim.Stack.Microsoft.Devices.Profiles;

internal static class Map
{
    private static bool _clientInProgress;
    private static bool _serverInProgress;

    private static string _deviceAddress = "";
    private static BluetoothMasClientSession _cachedClient;
    private static bool _cachedClientInProgress;

    private static async Task<ErrorData> MapClientActionAsync(
        string address,
        OperationToken token,
        Func<BluetoothMasClientSession, Task> action,
        bool calledByServer = false
    )
    {
        if (_cachedClient is { Connected: true })
            return await MapCachedClientActionAsync(address, token, action);

        if (_clientInProgress)
            return Errors.ErrorOperationInProgress.WrapError(
                new Dictionary<string, object>
                {
                    { "operation", "Message Access Client" },
                    { "exception", "A Message Access Profile client session is in progress" }
                }
            );

        try
        {
            _clientInProgress = true;

            using var device = await DeviceUtils.GetBluetoothDeviceWithService(
                address,
                BluetoothService.MessageAccessServer
            );
            using BluetoothMasClientSession client = new(device, token.LinkedCancelTokenSource);

            await client.ConnectAsync();
            if (client.ObexClient == null)
                throw new Exception("MAP ObexClient is null on connect");

            await action.Invoke(client);

            if (calledByServer)
            {
                _cachedClient = client;
                _deviceAddress = address;

                token.Wait();

                _deviceAddress = "";
                _cachedClient?.Dispose();
                _cachedClient = null;
            }
        }
        catch (Exception e)
        {
            return Errors.ErrorDeviceMessageAccessClient.WrapError(
                new Dictionary<string, object> { { "exception", e.Message } }
            );
        }
        finally
        {
            _clientInProgress = false;
        }

        return Errors.ErrorNone;
    }

    private static async Task<ErrorData> MapCachedClientActionAsync(
        string address,
        OperationToken token,
        Func<BluetoothMasClientSession, Task> action
    )
    {
        if (_cachedClientInProgress || address != _deviceAddress)
            return Errors.ErrorOperationInProgress.WrapError(
                new Dictionary<string, object>
                {
                    { "operation", "Message Access Client" },
                    { "exception", "A Message Access Profile client session is in progress" }
                }
            );

        try
        {
            _cachedClientInProgress = true;
            if (_cachedClient.ObexClient == null)
                throw new Exception("MAP ObexClient (cached) is null");

            await action.Invoke(_cachedClient);
        }
        catch (Exception e)
        {
            return Errors.ErrorDeviceMessageAccessClient.WrapError(
                new Dictionary<string, object> { { "exception", e.Message + " (cached)" } }
            );
        }
        finally
        {
            _cachedClientInProgress = false;
        }

        return Errors.ErrorNone;
    }

    private static async Task<string> SetPathAndGetFolderNameAsync(
        BluetoothMasClientSession client,
        string folderPath,
        bool cdIntoFolderName
    )
    {
        var folderName = "root";

        await client.ObexClient.SetFolderAsync(SetPathMode.BackToRoot);

        if (!string.IsNullOrEmpty(folderPath))
        {
            var folders = folderPath.TrimEnd('/').Split('/').ToList();
            folders.RemoveAll(string.IsNullOrEmpty);
            if (folders.Count > 0)
            {
                folderName = folders[^1];

                if (!cdIntoFolderName)
                    folders.RemoveAt(folders.Count - 1);
            }

            foreach (var folder in folders)
                await client.ObexClient.SetFolderAsync(SetPathMode.EnterFolder, folder);
        }

        return folderName;
    }

    internal static async Task<ErrorData> StartNotificationEventServerAsync(
        OperationToken token,
        string address
    )
    {
        if (_serverInProgress)
            return Errors.ErrorOperationInProgress.WrapError(
                new Dictionary<string, object>
                {
                    { "operation", "Message Access Server" },
                    { "exception", "A Message Access Profile server session is in progress" }
                }
            );

        try
        {
            _serverInProgress = true;

            using BluetoothMnsServerSession server = new(token.LinkedCancelTokenSource);
            var clientError = Errors.ErrorNone;
            var serverError = Errors.ErrorNone;

            await server.StartServerAsync();

            server.ClientAccepted += (_, args) =>
            {
                args.ObexServer.MessageReceived += (_, e) =>
                {
                    Output.Event(
                        e.ToMessageEvent(DeviceUtils.HostAddress(args.ClientInfo.HostName.RawName)),
                        token
                    );
                };
            };
            server.ClientDisconnected += (_, args) =>
            {
                if (args.ObexServerException != null)
                    serverError = Errors.ErrorDeviceMessageAccessServer.AddMetadata(
                        "exception",
                        args.ObexServerException.Message
                    );
            };

            Task.Run(async () =>
                {
                    var error = await MapClientActionAsync(
                        address,
                        token,
                        async client => { await client.ObexClient.SetNotificationRegistrationAsync(true); },
                        true
                    );
                    if (error != Errors.ErrorNone)
                    {
                        clientError = error;
                        token.Release();
                    }
                })
                .Wait();

            if (clientError != Errors.ErrorNone)
                return clientError;

            if (serverError != Errors.ErrorNone)
                return serverError;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            return Errors.ErrorDeviceMessageAccessServer.WrapError(
                new Dictionary<string, object> { { "exception", e.Message } }
            );
        }
        finally
        {
            _serverInProgress = false;
        }

        return Errors.ErrorNone;
    }

    internal static async Task<(MessageListingModel, ErrorData)> GetMessagesAsync(
        OperationToken token,
        string address,
        string folderPath = "telecom",
        ushort fromIndex = 0,
        ushort maxMessageCount = 0
    )
    {
        List<MessageListing> messages = [];

        var error = await MapClientActionAsync(
            address,
            token,
            async client =>
            {
                var folderName = await SetPathAndGetFolderNameAsync(client, folderPath, false);

                if (fromIndex <= 0)
                    fromIndex = 0;

                if (maxMessageCount <= 0)
                {
                    maxMessageCount = await client.ObexClient.GetMessageListingSizeAsync(
                        folderName
                    );
                    if (maxMessageCount == 0)
                        maxMessageCount = 1024;
                }

                messages = await client.ObexClient.GetMessagesListingAsync(
                    fromIndex,
                    maxMessageCount,
                    folderName
                );
            }
        );

        return error != Errors.ErrorNone
            ? (messages.ToMessageListing(), error)
            : (messages.ToMessageListing(), Errors.ErrorNone);
    }

    internal static async Task<(BMessagesModel, ErrorData)> ShowMessageAsync(
        OperationToken token,
        string address,
        string handle
    )
    {
        BMessage message = new(BMessageStatus.Unread, "", "", new VCard(), "", 0, "");

        var error = await MapClientActionAsync(
            address,
            token,
            async client => { message = await client.ObexClient.GetMessageAsync(handle); }
        );

        return error != Errors.ErrorNone
            ? (message.ToBMessage(), error)
            : (message.ToBMessage(), Errors.ErrorNone);
    }

    internal static async Task<(GenericResult<int>, ErrorData)> GetTotalMessageCountAsync(
        OperationToken token,
        string address,
        string folderPath
    )
    {
        var count = 0;

        var error = await MapClientActionAsync(
            address,
            token,
            async client =>
            {
                var folderName = await SetPathAndGetFolderNameAsync(client, folderPath, false);
                count = await client.ObexClient.GetMessageListingSizeAsync(folderName);
            }
        );

        return error != Errors.ErrorNone
            ? (GenericResult<int>.Empty(), error)
            : (
                count.ToResult($"Total messages in {folderPath}", "totalMessageCount"),
                Errors.ErrorNone
            );
    }

    internal static async Task<(GenericResult<List<string>>, ErrorData)> GetMessageFoldersAsync(
        OperationToken token,
        string address,
        string folderPath = ""
    )
    {
        List<string> folders = [];

        var error = await MapClientActionAsync(
            address,
            token,
            async client =>
            {
                await SetPathAndGetFolderNameAsync(client, folderPath, true);
                folders = await client.ObexClient.GetAllChildrenFoldersAsync();
            }
        );

        return error != Errors.ErrorNone
            ? (GenericResult<List<string>>.Empty(), error)
            : (
                folders.ToResult($" = Folders in {folderPath} =", "messageFolders"),
                Errors.ErrorNone
            );
    }
}