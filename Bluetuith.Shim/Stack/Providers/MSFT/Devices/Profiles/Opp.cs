using Bluetuith.Shim.Executor;
using Bluetuith.Shim.Executor.OutputHandler;
using Bluetuith.Shim.Stack.Events;
using Bluetuith.Shim.Types;
using GoodTimeStudio.MyPhone.OBEX.Opp;
using InTheHand.Net.Bluetooth;
using Windows.Devices.Bluetooth;

namespace Bluetuith.Shim.Stack.Providers.MSFT.Devices.Profiles;

internal sealed class Opp
{
    private static bool _clientInProgress = false;
    private static bool _serverInProgress = false;

    public static async Task<ErrorData> StartFileTransferClientAsync(OperationToken token, string address, params string[] files)
    {
        var deviceFound = false;

        if (_clientInProgress)
        {
            return Errors.ErrorOperationInProgress.WrapError(new()
                {
                    {"operation", "Object Push Client" },
                    {"exception", "An Object Push client instance is in progress"}
                });
        }

        DeviceConnectionStatusEvent deviceConnectionEvent = new(address, StackEventCode.FileTransferClientEventCode);

        try
        {
            _clientInProgress = true;

            using BluetoothDevice device = await DeviceUtils.GetBluetoothDeviceWithService(address, BluetoothService.ObexObjectPush);
            using BluetoothOppClientSession client = new(device, token.CancelTokenSource);
            deviceFound = true;

            Output.Event(
                deviceConnectionEvent with { State = ConnectionStatusEvent.ConnectionStatus.DeviceConnecting },
                token
            );

            await client.ConnectAsync();
            if (client.ObexClient == null)
            {
                throw new Exception("OPP ObexClient is null on connect");
            }

            client.ObexClient.TransferEventHandler += (s, e) =>
            {
                Output.Event(new FileTransferEvent(StackEventCode.FileTransferClientEventCode)
                {
                    Address = address,
                    FileName = e.FileName,
                    FileSize = e.FileSize,
                    BytesTransferred = e.BytesTransferred,
                }, token);
            };

            Output.Event(
                deviceConnectionEvent with { State = ConnectionStatusEvent.ConnectionStatus.DeviceConnected },
                token
            );

            foreach (var file in files)
            {
                if (await client.ObexClient.SendFile(file))
                    return Errors.ErrorNone;
            }
        }
        catch (OperationCanceledException)
        {
            return Errors.ErrorOperationCancelled;
        }
        catch (Exception e)
        {
            return StackErrors.ErrorDeviceFileTransferClient.WrapError(new() {
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

    public static async Task<ErrorData> StartFileTransferServerAsync(OperationToken token, string destinationDirectory)
    {
        if (_serverInProgress)
        {
            return Errors.ErrorOperationInProgress.WrapError(new()
                {
                    {"operation", "Object Push Server" },
                    {"exception", "An Object Push server instance is in progress"}
                });
        }

        ServerConnectionStatusEvent serverConnectionEvent = new("Object Push", StackEventCode.FileTransferServerEventCode);

        try
        {

            _serverInProgress = true;

            using BluetoothOppServerSession server = new(destinationDirectory, token.CancelTokenSource);
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
                DeviceConnectionStatusEvent deviceConnectionEvent = new(
                    DeviceUtils.HostAddress(args.ClientInfo.HostName.RawName),
                    StackEventCode.FileTransferClientEventCode
                );
                Output.Event(
                    deviceConnectionEvent with { State = ConnectionStatusEvent.ConnectionStatus.DeviceConnected },
                    token
                );

                args.ObexServer.ReceiveTransferEventHandler += (s, e) =>
                {
                    Output.Event(new FileTransferEvent(StackEventCode.FileTransferServerEventCode)
                    {
                        Address = DeviceUtils.HostAddress(args.ClientInfo.HostName.RawName),
                        FileName = e.FileName,
                        FileSize = e.FileSize,
                        BytesTransferred = e.BytesTransferred,
                    }, token);
                };
            };
            server.ClientDisconnected += (sender, args) =>
            {
                DeviceConnectionStatusEvent deviceConnectionEvent = new(
                    DeviceUtils.HostAddress(args.ClientInfo.HostName.RawName),
                    StackEventCode.FileTransferClientEventCode
                );
                Output.Event(
                    deviceConnectionEvent with { State = ConnectionStatusEvent.ConnectionStatus.DeviceDisconnected },
                    token
                );

                if (args.ObexServerException != null)
                {
                    Output.Error(StackErrors.ErrorDeviceFileTransferServer.WrapError(new() {
                            {"exception", args.ObexServerException.Message}
                    }), token);
                }
            };

            token.CancelToken.WaitHandle.WaitOne();
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            return StackErrors.ErrorDeviceFileTransferServer.WrapError(new() {
                {"exception", e.Message}
            });
        }
        finally
        {
            Output.Event(serverConnectionEvent with { State = ConnectionStatusEvent.ConnectionStatus.ServerStopped }, token);
            _serverInProgress = false;
        }

        return Errors.ErrorNone;
    }
}