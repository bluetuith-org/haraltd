using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.Managers;
using Haraltd.Operations.OutputStream;
using Haraltd.Stack.Base;
using Haraltd.Stack.Base.Profiles.Opp;
using Haraltd.Stack.Obex.Profiles.Opp;
using InTheHand.Net;

namespace Haraltd.Stack.Obex.SessionManager;

public class OppSessionManager(IBluetoothStack stack, IServer server) : IOppSessionManager
{
    private readonly OppSessions _sessions = new(stack);

    private readonly string _transferCacheDir = Path.Combine(server.AppCacheDirectory, "transfers");

    public async ValueTask<ErrorData> StartFileTransferSessionAsync(
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

            if (_sessions.GetClientSession(token, address, out _, out _))
                return Errors.ErrorDeviceFileTransferSession.AddMetadata(
                    "exception",
                    "An Object Push Client session is in progress"
                );

            return await _sessions.StartAsync(
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

    public (FileTransferModel, ErrorData) QueueFileSend(
        OperationToken token,
        BluetoothAddress address,
        string filepath
    )
    {
        return !_sessions.GetClientSession(token, address, out var client, out var error)
            ? (null, error)
            : client.QueueFileSend(filepath);
    }

    public async ValueTask<ErrorData> StopFileTransferSessionAsync(
        OperationToken token,
        BluetoothAddress address
    )
    {
        return await _sessions.StopAsync(token, address);
    }

    public async ValueTask<ErrorData> CancelFileTransferSessionAsync(
        OperationToken token,
        BluetoothAddress address
    )
    {
        if (!_sessions.GetClientSession(token, address, out var client, out var error))
        {
            _sessions.GetServerTransferSession(token, address, out var serverTransfer, out error);
            if (serverTransfer != null)
                await serverTransfer.CancelTransferAsync();

            return error;
        }

        if (client != null)
            await client.CancelTransferAsync();

        return error;
    }

    public async ValueTask<ErrorData> StartFileTransferServerAsync(
        OperationToken token,
        BluetoothAddress adapterAddress,
        string destinationDirectory = ""
    )
    {
        if (string.IsNullOrEmpty(destinationDirectory))
            destinationDirectory = _transferCacheDir;

        try
        {
            if (!Path.Exists(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);
        }
        catch (Exception ex)
        {
            return Errors.ErrorDeviceFileTransferSession.AddMetadata("exception", ex.Message);
        }

        if (_sessions.GetServerSession(token, adapterAddress, out _, out _))
            return Errors.ErrorOperationInProgress.WrapError(
                new Dictionary<string, object>
                {
                    { "operation", "Object Push Server" },
                    { "exception", "An Object Push server instance is in progress" },
                }
            );

        return await _sessions.StartAsync(
            token,
            adapterAddress,
            new OppServer(
                new OppServerProperties(
                    destinationDirectory,
                    data =>
                    {
                        var tok = OperationManager.GenerateToken(0, token.CancelTokenSource.Token);

                        return Output.ConfirmAuthentication(
                            new OppAuthenticationEvent(10000, data, tok),
                            AuthenticationManager.AuthAgentType.Obex
                        );
                    }
                )
            )
        );
    }

    public async ValueTask<ErrorData> StopFileTransferServerAsync(
        OperationToken token,
        BluetoothAddress adapterAddress
    )
    {
        return await _sessions.StopAsync(token, adapterAddress);
    }

    public bool IsServerSessionExist(
        OperationToken token,
        BluetoothAddress address,
        out OppServer session
    )
    {
        session = null;

        if (_sessions.GetSession(token, address, true, out OppServer oppSession))
            session = oppSession;

        return session != null;
    }
}
