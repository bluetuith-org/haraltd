using Bluetuith.Shim.Executor;
using Bluetuith.Shim.Executor.OutputHandler;
using Bluetuith.Shim.Stack.Events;
using Bluetuith.Shim.Stack.Models;
using Bluetuith.Shim.Types;
using GoodTimeStudio.MyPhone.OBEX;
using GoodTimeStudio.MyPhone.OBEX.Map;
using InTheHand.Net.Bluetooth;
using MixERP.Net.VCards;
using Windows.Devices.Bluetooth;

namespace Bluetuith.Shim.Stack.Providers.MSFT.Devices.Profiles;

internal sealed class Map
{
#nullable disable
    private static bool _clientInProgress = false;
    private static bool _serverInProgress = false;

    private static string _deviceAddress = "";
    private static BluetoothMasClientSession _cachedClient;
    private static bool _cachedClientInProgress = false;

    private static async Task<ErrorData> MapClientActionAsync(
            string address,
            OperationToken token,
            Func<BluetoothMasClientSession, Task> action,
            bool calledByServer = false
        )
    {
        if (_cachedClient != null && _cachedClient.Connected)
        {
            return await MapCachedClientActionAsync(address, token, action);
        }

        if (_clientInProgress)
        {
            return Errors.ErrorOperationInProgress.WrapError(new()
                {
                    {"operation", "Message Access Client" },
                    {"exception", $"A Message Access Profile client session is in progress" }
                });
        }

        var deviceFound = false;
        DeviceConnectionStatusEvent deviceConnectionEvent = new(address, StackEventCode.MessageAccessClientEventCode);

        try
        {
            _clientInProgress = true;

            using BluetoothDevice device = await DeviceUtils.GetBluetoothDeviceWithService(address, BluetoothService.MessageAccessServer);
            using BluetoothMasClientSession client = new(device, token.CancelTokenSource);
            deviceFound = true;

            Output.Event(
                deviceConnectionEvent with { State = ConnectionStatusEvent.ConnectionStatus.DeviceConnecting },
                token
            );

            await client.ConnectAsync();
            if (client.ObexClient == null)
            {
                throw new Exception("MAP ObexClient is null on connect");
            }

            Output.Event(
                deviceConnectionEvent with { State = ConnectionStatusEvent.ConnectionStatus.DeviceConnected },
                token
            );

            await action.Invoke(client);

            if (calledByServer)
            {
                _cachedClient = client;
                _deviceAddress = address;

                token.CancelToken.WaitHandle.WaitOne();

                _deviceAddress = "";
                _cachedClient?.Dispose();
                _cachedClient = null;
            }
        }
        catch (Exception e)
        {
            return StackErrors.ErrorDeviceMessageAccessClient.WrapError(new()
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

    private static async Task<ErrorData> MapCachedClientActionAsync(
        string address,
        OperationToken token,
        Func<BluetoothMasClientSession, Task> action
    )
    {
        if (_cachedClientInProgress || address != _deviceAddress)
        {
            return Errors.ErrorOperationInProgress.WrapError(new()
                {
                    {"operation", "Message Access Client" },
                    {"exception", $"A Message Access Profile client session is in progress" }
                });
        }

        try
        {
            _cachedClientInProgress = true;
            if (_cachedClient.ObexClient == null)
            {
                throw new Exception("MAP ObexClient (cached) is null");
            }

            await action.Invoke(_cachedClient);
        }
        catch (Exception e)
        {
            return StackErrors.ErrorDeviceMessageAccessClient.WrapError(new()
            {
                {"exception", e.Message + " (cached)"}
            });
        }
        finally { _cachedClientInProgress = false; }

        return Errors.ErrorNone;
    }

    private static async Task<string> SetPathAndGetFolderNameAsync(BluetoothMasClientSession client, string folderPath, bool cdIntoFolderName)
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
                {
                    folders.RemoveAt(folders.Count - 1);
                }
            }

            foreach (var folder in folders)
            {
                await client.ObexClient.SetFolderAsync(SetPathMode.EnterFolder, folder);
            }
        }

        return folderName;
    }

    public static async Task<ErrorData> StartNotificationEventServerAsync(OperationToken token, string address)
    {
        if (_serverInProgress)
        {
            return Errors.ErrorOperationInProgress.WrapError(new()
                {
                    {"operation", "Message Access Server" },
                    {"exception", $"A Message Access Profile server session is in progress" }
                });
        }

        ServerConnectionStatusEvent serverConnectionEvent = new("Message Access", StackEventCode.MessageAccessServerEventCode);

        try
        {
            _serverInProgress = true;

            using BluetoothMnsServerSession server = new(token.CancelTokenSource);
            ErrorData clientError = Errors.ErrorNone;
            ErrorData serverError = Errors.ErrorNone;

            Output.Event(
                serverConnectionEvent with { State = ConnectionStatusEvent.ConnectionStatus.ServerStarting },
                token
            );

            await server.StartServerAsync();
            Output.Event(
                serverConnectionEvent with { State = ConnectionStatusEvent.ConnectionStatus.ServerStarted },
                token
            );

            server.ClientAccepted += (sender, args) =>
            {
                args.ObexServer.MessageReceived += (s, e) =>
                {
                    Output.Event(
                        new MessageReceivedEvent(DeviceUtils.HostAddress(args.ClientInfo.HostName.RawName), e),
                        token
                    );
                };
            };
            server.ClientDisconnected += (sender, args) =>
            {
                if (args.ObexServerException != null)
                {
                    serverError = StackErrors.ErrorDeviceMessageAccessServer.WrapError(new() {
                            {"exception", args.ObexServerException.Message}
                    });
                }
            };

            Task.Run(async () =>
            {
                ErrorData error = await MapClientActionAsync(address, token, async (client) =>
                {
                    await client.ObexClient.SetNotificationRegistrationAsync(true);
                }, true);
                if (error != Errors.ErrorNone)
                {
                    clientError = error;
                    token.CancelTokenSource.Cancel();
                }
            }).Wait();

            if (clientError != Errors.ErrorNone)
            {
                return clientError;
            }

            if (serverError != Errors.ErrorNone)
            {
                return serverError;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            return StackErrors.ErrorDeviceMessageAccessServer.WrapError(new()
            {
                {"exception", e.Message}
            });
        }
        finally { _serverInProgress = false; }

        return Errors.ErrorNone;
    }

    public static async Task<(MessageListingModel, ErrorData)> GetMessagesAsync(OperationToken token, string address, string folderPath = "telecom", ushort fromIndex = 0, ushort maxMessageCount = 0)
    {
        List<MessageListing> messages = [];

        ErrorData error = await MapClientActionAsync(address, token, async (client) =>
        {
            var folderName = await SetPathAndGetFolderNameAsync(client, folderPath, false);

            if (fromIndex <= 0)
            {
                fromIndex = 0;
            }

            if (maxMessageCount <= 0)
            {
                maxMessageCount = await client.ObexClient.GetMessageListingSizeAsync(folderName);
                if (maxMessageCount == 0)
                {
                    maxMessageCount = 1024;
                }
            }

            messages = await client.ObexClient.GetMessagesListingAsync(fromIndex, maxMessageCount, folderName);
        });

        return error != Errors.ErrorNone ? (new MessageListingModel(messages), error) : (new MessageListingModel(messages), Errors.ErrorNone);
    }

    public static async Task<(BMessageModel, ErrorData)> ShowMessageAsync(OperationToken token, string address, string handle)
    {
        BMessage message = new(BMessageStatus.UNREAD, "", "", new VCard(), "", 0, "");

        ErrorData error = await MapClientActionAsync(address, token, async (client) =>
        {
            message = await client.ObexClient.GetMessageAsync(handle);
        });

        return error != Errors.ErrorNone ? (new BMessageModel(message), error) : (new BMessageModel(message), Errors.ErrorNone);
    }

    public static async Task<(GenericResult<int>, ErrorData)> GetTotalMessageCountAsync(OperationToken token, string address, string folderPath)
    {
        var count = 0;

        ErrorData error = await MapClientActionAsync(address, token, async (client) =>
        {
            var folderName = await SetPathAndGetFolderNameAsync(client, folderPath, false);
            count = await client.ObexClient.GetMessageListingSizeAsync(folderName);
        });

        return error != Errors.ErrorNone
            ? (GenericResult<int>.Empty(), error)
            : (count.ToResult($"Total messages in {folderPath}", "totalMessageCount"), Errors.ErrorNone);
    }

    public static async Task<(GenericResult<List<string>>, ErrorData)> GetMessageFoldersAsync(OperationToken token, string address, string folderPath = "")
    {
        List<string> folders = [];

        ErrorData error = await MapClientActionAsync(address, token, async (client) =>
        {
            await SetPathAndGetFolderNameAsync(client, folderPath, true);
            folders = await client.ObexClient.GetAllChildrenFoldersAsync();
        });

        return error != Errors.ErrorNone
            ? (GenericResult<List<string>>.Empty(), error)
            : ((GenericResult<List<string>>, ErrorData))(folders.ToResult($" = Folders in {folderPath} =", "messageFolders"), Errors.ErrorNone);
    }
#nullable restore
}
