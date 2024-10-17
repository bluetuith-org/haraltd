using Bluetuith.Shim.Executor.OutputHandler;
using Bluetuith.Shim.Stack;
using Bluetuith.Shim.Stack.Models;
using Bluetuith.Shim.Types;
using DotMake.CommandLine;

namespace Bluetuith.Shim.Executor;

[CliCommand(Description = "Bluetooth shim and command-line utility.")]
public sealed class Commands
{
    public readonly IStack Stack = AvailableStacks.CurrentStack;

    [CliOption(Hidden = true, Name = "--request-id")]
    public int RequestId { get; set; } = 0;

    [CliOption(Hidden = true, Name = "--operation-id")]
    public int OperationId { get; set; } = 0;

    [CliCommand(Description = "Start a RPC session or perform functions related to an ongoing RPC operation within a session.")]
    public class Rpc
    {
        public class OperationIdParameter
        {
            [CliOption(Required = true, Description = "The ID of the ongoing operation.")]
            public int OperationId { get; set; }
        }

        [CliCommand(Description = "Start a RPC session with the provided path to a Unix Domain Socket. Do not create the socket, the socket will be created automatically using the provided full path and socket name.")]
        public class StartSession
        {
            [CliOption(Required = true, Description = "The full path and the socket name to be created and listened on. For example: 'C:\\Users\\user\\AppData\\Local\\Temp\\socket.sock'")]
            public string SocketPath { get; set; } = "";

            public required Commands RootCommand { get; set; }

            public async Task<int> RunAsync(CliContext context)
            {
                using var token = OperationToken.GetLinkedToken(
                    RootCommand.OperationId, RootCommand.RequestId, context.CancellationToken
                );

                ErrorData error = Output.StartSocketServer(SocketPath, token);
                if (error != Errors.ErrorNone)
                {
                    Console.WriteLine(error.ToConsoleString());
                }

                return await Task.FromResult(error.Code.Value);
            }
        }

        [CliCommand(Description = "Show the current version information of the RPC server.")]
        public class Version
        {
            public required Commands RootCommand { get; set; }

            public int Run(CliContext context)
            {
                using var token = OperationToken.GetLinkedToken(
                    RootCommand.OperationId, RootCommand.RequestId, context.CancellationToken
                );

                System.Version version = typeof(Commands).Assembly.GetName().Version ?? new();

                return Output.Result(version.ToString().ToResult("Version", "version"), Errors.ErrorNone, token);
            }
        }

        [CliCommand(Description = "Set the response for a pending authentication request attached to an operation ID.")]
        public class Auth : OperationIdParameter
        {
            [CliOption(Required = true, Description = "The response to sent to the authentication request.")]
            public string Response { get; set; } = "";

            public int Run(CliContext context)
            {
                Output.ReplyToAuthenticationRequest(OperationId, Response);

                return 0;
            }
        }

        [CliCommand(Description = "Cancel an ongoing operation with its operation ID.")]
        public class Cancel : OperationIdParameter
        {
            public async Task<int> RunAsync(CliContext context)
            {
                await Operation.CancelAsync(OperationId);

                return 0;
            }
        }
    }

    [CliCommand(Description = "Perform operations on the Bluetooth adapter.")]
    public class Adapter
    {
        [CliCommand(Description = "Get information about the Bluetooth adapter.")]
        public class Info
        {
            public required Commands RootCommand { get; set; }

            public async Task<int> RunAsync(CliContext context)
            {
                using var token = OperationToken.GetLinkedToken(
                    RootCommand.OperationId, RootCommand.RequestId, context.CancellationToken
                );

                (AdapterModel adapter, ErrorData error) = await RootCommand.Stack.GetAdapterAsync();

                return Output.Result(adapter, error, token);
            }
        }

        [CliCommand(Description = "Start a device discovery.")]
        public class StartDiscovery
        {
            [CliOption(Description = "A value in seconds which determines when the device discovery will finish.")]
            public int Timeout { get; set; }

            public required Commands RootCommand { get; set; }

            public async Task<int> RunAsync(CliContext context)
            {
                using var token = OperationToken.GetLinkedToken(
                    RootCommand.OperationId, RootCommand.RequestId, context.CancellationToken
                );

                ErrorData error = await RootCommand.Stack.StartDeviceDiscoveryAsync(token, Timeout);

                return Output.Error(error, token);
            }
        }

        [CliCommand(Description = "Get the list of paired devices.")]
        public class GetPairedDevices
        {
            public required Commands RootCommand { get; set; }

            public async Task<int> RunAsync(CliContext context)
            {
                using var token = OperationToken.GetLinkedToken(
                    RootCommand.OperationId, RootCommand.RequestId, context.CancellationToken
                );

                (GenericResult<List<DeviceModel>> devices, ErrorData error) = await RootCommand.Stack.GetPairedDevices();

                return Output.Result(devices, error, token);
            }
        }

        [CliCommand(Description = "Sets the power state of the adapter.")]
        public class SetPowerState
        {
            public enum PowerState : int
            {
                On,
                Off,
            }

            [CliArgument(Description = "The adapter power state to set.")]
            public PowerState State { get; set; }

            public required Commands RootCommand { get; set; }

            public async Task<int> RunAsync(CliContext context)
            {
                using var token = OperationToken.GetLinkedToken(
                    RootCommand.OperationId, RootCommand.RequestId, context.CancellationToken
                );

                ErrorData error = await RootCommand.Stack.SetAdapterStateAsync(State == PowerState.On);

                return Output.Error(error, token);
            }
        }
    }

    [CliCommand(Description = "Perform operations on a Bluetooth device.")]
    public class Device
    {
        public class AddressParameter
        {
            [CliOption(Required = true, Description = "The Bluetooth address of the device.")]
            public string Address { get; set; } = "";
        }

        [CliCommand(Description = "Get information about a Bluetooth device.")]
        public class Info : AddressParameter
        {
            [CliOption(Description = "Start a device discovery for this device address if it is not found locally.")]
            public bool IssueInquiry { get; set; }

            [CliOption(Description = "Show the list of L2CAP services advertised by the device.")]
            public bool ShowServices { get; set; }

            public required Commands RootCommand { get; set; }

            public async Task<int> RunAsync(CliContext context)
            {
                using var token = OperationToken.GetLinkedToken(
                    RootCommand.OperationId, RootCommand.RequestId, context.CancellationToken
                );

                (DeviceModel device, ErrorData error) = await RootCommand.Stack.GetDeviceAsync(
                    token,
                    Address,
                    IssueInquiry,
                    ShowServices
                );

                return Output.Result(device, error, token);
            }
        }

        [CliCommand(Description = "Pair with a Bluetooth device.")]
        public class Pair : AddressParameter
        {
            [CliOption(Description = "The maximum amount of time in seconds that a pairing request can wait for a reply during authentication.")]
            public int Timeout { get; set; } = 10;

            public required Commands RootCommand { get; set; }

            public async Task<int> RunAsync(CliContext context)
            {
                using var token = OperationToken.GetLinkedToken(
                    RootCommand.OperationId, RootCommand.RequestId, context.CancellationToken
                );

                ErrorData error = await RootCommand.Stack.PairAsync(token, Address, Timeout);

                return Output.Error(error, token);
            }
        }

        [CliCommand(Description = "Connect to a Bluetooth device.")]
        public class Connect
        {
            [CliCommand(Name = "a2dp", Description = "Start an Advanced Audio Distribution Profile session.")]
            public class A2dp
            {
                [CliCommand(Description = "Start an A2DP session with a Bluetooth device.")]
                public class StartSession : AddressParameter
                {
                    public required Commands RootCommand { get; set; }

                    public async Task<int> RunAsync(CliContext context)
                    {
                        using var token = OperationToken.GetLinkedToken(
                            RootCommand.OperationId, RootCommand.RequestId, context.CancellationToken
                        );

                        ErrorData error = await RootCommand.Stack.StartAudioSessionAsync(
                            token,
                            Address
                        );

                        return Output.Error(error, token);
                    }
                }
            }

            [CliCommand(Description = "Perform a Phonebook Profile based operation.")]
            public class Pbap
            {
                [CliCommand(Description = "Get the contacts list from a device.")]
                public class GetAllContacts : AddressParameter
                {
                    public required Commands RootCommand { get; set; }

                    public async Task<int> RunAsync(CliContext context)
                    {
                        using var token = OperationToken.GetLinkedToken(
                            RootCommand.OperationId, RootCommand.RequestId, context.CancellationToken
                        );

                        (VcardModel vcard, ErrorData error) = await RootCommand.Stack.GetAllContactsAsync(
                            token,
                            Address
                        );

                        return Output.Result(vcard, error, token);
                    }
                }

                [CliCommand(Description = "Get the combined call history from a device.")]
                public class GetCombinedCalls : AddressParameter
                {
                    public required Commands RootCommand { get; set; }

                    public async Task<int> RunAsync(CliContext context)
                    {
                        using var token = OperationToken.GetLinkedToken(
                            RootCommand.OperationId, RootCommand.RequestId, context.CancellationToken
                        );

                        (VcardModel vcard, ErrorData error) = await RootCommand.Stack.GetCombinedCallHistoryAsync(
                            token,
                            Address
                        );

                        return Output.Result(vcard, error, token);
                    }
                }

                [CliCommand(Description = "Get the incoming call history from a device.")]
                public class GetIncomingCalls : AddressParameter
                {
                    public required Commands RootCommand { get; set; }

                    public async Task<int> RunAsync(CliContext context)
                    {
                        using var token = OperationToken.GetLinkedToken(
                            RootCommand.OperationId, RootCommand.RequestId, context.CancellationToken
                        );

                        (VcardModel vcard, ErrorData error) = await RootCommand.Stack.GetIncomingCallsHistoryAsync(
                            token,
                            Address
                        );

                        return Output.Result(vcard, error, token);
                    }
                }

                [CliCommand(Description = "Get the outgoing call history from a device.")]
                public class GetOutgoingCalls : AddressParameter
                {
                    public required Commands RootCommand { get; set; }

                    public async Task<int> RunAsync(CliContext context)
                    {
                        using var token = OperationToken.GetLinkedToken(
                            RootCommand.OperationId, RootCommand.RequestId, context.CancellationToken
                        );

                        (VcardModel vcard, ErrorData error) = await RootCommand.Stack.GetOutgoingCallsHistoryAsync(
                            token,
                            Address
                        );

                        return Output.Result(vcard, error, token);
                    }
                }

                [CliCommand(Description = "Get the missed call history from a device.")]
                public class GetMissedCalls : AddressParameter
                {
                    public required Commands RootCommand { get; set; }

                    public async Task<int> RunAsync(CliContext context)
                    {
                        using var token = OperationToken.GetLinkedToken(
                            RootCommand.OperationId, RootCommand.RequestId, context.CancellationToken
                        );

                        (VcardModel vcard, ErrorData error) = await RootCommand.Stack.GetMissedCallsAsync(
                            token,
                            Address
                        );

                        return Output.Result(vcard, error, token);
                    }
                }

                [CliCommand(Description = "Get the speed dial list from a device.")]
                public class GetSpeedDial : AddressParameter
                {
                    public required Commands RootCommand { get; set; }

                    public async Task<int> RunAsync(CliContext context)
                    {
                        using var token = OperationToken.GetLinkedToken(
                            RootCommand.OperationId, RootCommand.RequestId, context.CancellationToken
                        );

                        (VcardModel vcard, ErrorData error) = await RootCommand.Stack.GetSpeedDialAsync(
                            token,
                            Address
                        );

                        return Output.Result(vcard, error, token);
                    }
                }
            }

            [CliCommand(Description = "Perform a Message Access Profile based operation.")]
            public class Map
            {
                [CliCommand(Description = "Get a list of messages from a Bluetooth device.")]
                public class GetMessages : AddressParameter
                {
                    [CliOption(HelpName = "folder", Description = "A path to a folder in the remote device to list messages from, for example, 'telecom/msg/inbox'.")]
                    public string Folder { get; set; } = "telecom";

                    [CliOption(HelpName = "index", Description = "The index of the message within a folder listing, from where messages can start being listed.")]
                    public ushort StartIndex { get; set; }

                    [CliOption(HelpName = "count", Description = "The maximum amount of messages to display. Setting bigger values will lead to longer download times.")]
                    public ushort MaximumCount { get; set; }

                    public required Commands RootCommand { get; set; }

                    public async Task<int> RunAsync(CliContext context)
                    {
                        using var token = OperationToken.GetLinkedToken(
                            RootCommand.OperationId, RootCommand.RequestId, context.CancellationToken
                        );

                        (MessageListingModel list, ErrorData error) = await RootCommand.Stack.GetMessagesAsync(
                            token,
                            Address,
                            Folder,
                            StartIndex,
                            MaximumCount
                        );

                        return Output.Result(list, error, token);
                    }
                }

                [CliCommand(Description = "Get a list of message folders from a Bluetooth device.")]
                public class GetMessageFolders : AddressParameter
                {
                    [CliOption(HelpName = "folder", Description = "A path to a folder in the remote device to list folders from, for example, 'telecom/msg'.")]
                    public string Folder { get; set; } = "telecom";

                    public required Commands RootCommand { get; set; }

                    public async Task<int> RunAsync(CliContext context)
                    {
                        using var token = OperationToken.GetLinkedToken(
                            RootCommand.OperationId, RootCommand.RequestId, context.CancellationToken
                        );

                        (GenericResult<List<string>> folders, ErrorData error) = await RootCommand.Stack.GetMessageFoldersAsync(
                            token,
                            Address,
                            Folder
                        );

                        return Output.Result(folders, error, token);
                    }
                }

                [CliCommand(Description = "Show a message from a Bluetooth device.")]
                public class ShowMessage : AddressParameter
                {
                    [CliOption(HelpName = "handle", Required = true, Description = "A message handle of the message to be displayed. Use 'get-messages' to list message handles.")]
                    public string MessageHandle { get; set; } = "";

                    public required Commands RootCommand { get; set; }

                    public async Task<int> RunAsync(CliContext context)
                    {
                        using var token = OperationToken.GetLinkedToken(
                            RootCommand.OperationId, RootCommand.RequestId, context.CancellationToken
                        );

                        (BMessageModel message, ErrorData error) = await RootCommand.Stack.ShowMessageAsync(
                            token,
                            Address,
                            MessageHandle
                        );

                        return Output.Result(message, error, token);
                    }
                }

                [CliCommand(Description = "Listen for notification events from a Bluetooth device.")]
                public class StartNotificationServer : AddressParameter
                {
                    public required Commands RootCommand { get; set; }

                    public async Task<int> RunAsync(CliContext context)
                    {
                        using var token = OperationToken.GetLinkedToken(
                            RootCommand.OperationId, RootCommand.RequestId, context.CancellationToken
                        );

                        ErrorData error = await RootCommand.Stack.StartNotificationEventServerAsync(
                            token,
                            Address
                        );

                        return Output.Error(error, token);
                    }
                }
            }

            [CliCommand(Description = "Perform a Object Push Profile based operation.")]
            public class Opp
            {
                [CliCommand(Description = "Send file(s) to a Bluetooth device.")]
                public class SendFiles : AddressParameter
                {
                    [CliOption(HelpName = "file", Description = "A full path of the file to send. Use '-f' multiple times to send multiple files.")]
                    public List<string> File { get; set; } = [];

                    public required Commands RootCommand { get; set; }

                    public async Task<int> RunAsync(CliContext context)
                    {
                        using var token = OperationToken.GetLinkedToken(
                            RootCommand.OperationId, RootCommand.RequestId, context.CancellationToken
                        );

                        ErrorData error = await RootCommand.Stack.StartFileTransferClientAsync(
                            token,
                            Address,
                            File.ToArray()
                        );

                        return Output.Error(error, token);
                    }
                }

                [CliCommand(Description = "Start an Object Push server.")]
                public class StartServer
                {
                    [CliOption(HelpName = "directory", Description = "A full path to a directory to save incoming file transfers.")]
                    public string Directory { get; set; } = "";

                    public required Commands RootCommand { get; set; }

                    public async Task<int> RunAsync(CliContext context)
                    {
                        using var token = OperationToken.GetLinkedToken(
                            RootCommand.OperationId, RootCommand.RequestId, context.CancellationToken
                        );

                        ErrorData error = await RootCommand.Stack.StartFileTransferServerAsync(
                            token,
                            Directory
                        );

                        return Output.Error(error, token);
                    }
                }
            }
        }

        [CliCommand(Description = "Disconnect from a Bluetooth device.")]
        public class Disconnect : AddressParameter
        {
            public required Commands RootCommand { get; set; }

            public async Task<int> RunAsync(CliContext context)
            {
                using var token = OperationToken.GetLinkedToken(
                    RootCommand.OperationId, RootCommand.RequestId, context.CancellationToken
                );

                ErrorData error = await RootCommand.Stack.DisconnectDeviceAsync(Address);

                return Output.Error(error, token);
            }
        }

        [CliCommand(Description = "Remove a Bluetooth device.")]
        public class Remove : AddressParameter
        {
            public required Commands RootCommand { get; set; }

            public async Task<int> RunAsync(CliContext context)
            {
                using var token = OperationToken.GetLinkedToken(
                    RootCommand.OperationId, RootCommand.RequestId, context.CancellationToken
                );

                ErrorData error = await RootCommand.Stack.RemoveDeviceAsync(Address);

                return Output.Error(error, token);
            }
        }
    }
}