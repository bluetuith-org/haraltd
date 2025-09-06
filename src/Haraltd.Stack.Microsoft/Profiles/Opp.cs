using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.Managers;
using Haraltd.Operations.OutputStream;
using Haraltd.Stack.Microsoft.Adapters;
using Haraltd.Stack.Microsoft.Windows;
using Haraltd.Stack.Obex.Profiles.Opp;
using InTheHand.Net;

namespace Haraltd.Stack.Microsoft.Profiles;

internal static class Opp
{
    private static readonly OppSessions Sessions = new(WindowsStack.Instance);

    private static readonly string TransferCacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "haraltd",
        "transfers"
    );

    internal static async Task<ErrorData> StartFileTransferSessionAsync(
        OperationToken token,
        BluetoothAddress address
    )
    {
        try
        {
            if (!token.HasClientId)
                return Errors.ErrorDeviceFileTransferSession.AddMetadata(
                    "exception",
                    "No client ID found"
                );

            if (Sessions.GetClientSession(token, address, out _, out _))
                return Errors.ErrorDeviceFileTransferSession.AddMetadata(
                    "exception",
                    "An Object Push Client session is in progress"
                );

            return await Sessions.StartAsync(
                token,
                address,
                new OppClient(new OppClientProperties())
            );
        }
        catch (OperationCanceledException)
        {
            return Errors.ErrorOperationCancelled;
        }
        catch (Exception e)
        {
            return Errors.ErrorDeviceFileTransferSession.AddMetadata("exception", e.Message);
        }
    }

    internal static (FileTransferModel, ErrorData) QueueFileSend(
        OperationToken token,
        BluetoothAddress address,
        string filepath
    )
    {
        return !Sessions.GetClientSession(token, address, out var client, out var error)
            ? (null, error)
            : client.QueueFileSend(filepath);
    }

    internal static async ValueTask<ErrorData> StopFileTransferSessionAsync(
        OperationToken token,
        BluetoothAddress address
    )
    {
        return await Sessions.StopAsync(token, address);
    }

    internal static async ValueTask<ErrorData> CancelFileTransferSessionAsync(
        OperationToken token,
        BluetoothAddress address
    )
    {
        return !Sessions.GetClientSession(token, address, out var client, out var error)
            ? error
            : await client.CancelTransferAsync();
    }

    internal static async Task<ErrorData> StartFileTransferServerAsync(
        OperationToken token,
        BluetoothAddress adapterAddress,
        string destinationDirectory = ""
    )
    {
        if (string.IsNullOrEmpty(destinationDirectory))
            destinationDirectory = TransferCacheDir;

        if (!Path.Exists(destinationDirectory))
            Directory.CreateDirectory(destinationDirectory);

        var (_, error) = WindowsAdapter.ConvertToAdapterModel(token);
        if (error != Errors.ErrorNone)
            return error;

        if (Sessions.GetServerSession(token, adapterAddress, out _, out _))
            return Errors.ErrorOperationInProgress.WrapError(
                new Dictionary<string, object>
                {
                    { "operation", "Object Push Server" },
                    { "exception", "An Object Push server instance is in progress" },
                }
            );

        return await Sessions.StartAsync(
            token,
            adapterAddress,
            new OppServer(
                new OppServerProperties(
                    destinationDirectory,
                    data =>
                    {
                        var tok = OperationManager.GenerateToken(0, token.CancelTokenSource.Token);

                        return Output.ConfirmAuthentication(
                            new WindowsOppAuthEvent(10000, data, tok),
                            AuthenticationManager.AuthAgentType.Obex
                        );
                    }
                )
            )
        );
    }

    internal static async Task<ErrorData> StopFileTransferServerAsync(
        OperationToken token,
        BluetoothAddress adapterAddress
    )
    {
        return await Sessions.StopAsync(token, adapterAddress);
    }

    internal static bool IsServerSessionExist(
        OperationToken token,
        BluetoothAddress address,
        out OppServer session
    )
    {
        session = null;

        if (Sessions.GetSession(token, address, out OppServer oppSession))
            session = oppSession;

        return session != null;
    }
}
