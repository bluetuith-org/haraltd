using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.Managers;
using Haraltd.Stack.Base;
using Haraltd.Stack.Base.Sockets;
using Haraltd.Stack.Obex.Headers;
using Haraltd.Stack.Obex.Packet;
using InTheHand.Net;

namespace Haraltd.Stack.Obex.Session;

public record ObexClientProperties : ObexSessionProperties;

public class ObexClient<T>(ObexService service, T properties) : IObexSession
    where T : ObexClientProperties, new()
{
    protected ISocket Socket;
    protected CancellationTokenSource Cts;

    protected T Properties => properties;

    public BluetoothAddress Address { get; set; }
    public OperationToken Token { get; set; }

    public IBluetoothStack Stack { get; set; }
    public IObexSessions Sessions { get; set; }

    public virtual async ValueTask<ErrorData> StartAsync()
    {
        (Socket, var error) = Stack.CreateSocket(service.GetServiceSocketOptions(Address));

        try
        {
            error = Socket != null ? await ConnectAsync() : error;
        }
        finally
        {
            if (Socket != null)
            {
                OperationManager.SetOperationProperties(Token, true, true);
                Cts = Token.LinkedCancelTokenSource;
            }
        }

        if (error != Errors.ErrorNone)
            _ = await StopAsync();

        return error;
    }

    public virtual async ValueTask<ErrorData> StopAsync()
    {
        Dispose();
        return await ValueTask.FromResult(Errors.ErrorNone);
    }

    public virtual async ValueTask<ErrorData> CancelTransferAsync()
    {
        await Cts.CancelAsync();

        return Errors.ErrorNone;
    }

    private async ValueTask<ErrorData> ConnectAsync()
    {
        try
        {
            await Socket.ConnectAsync();

            var packet = new ObexConnectPacket(service);
            packet.WriteToStream(Socket.Writer);
            await Socket.Writer.StoreAsync();

            var response = await ObexPacket.ReadFromStream<ObexConnectPacket>(Socket.Reader);
            if (response.Opcode.Operation != ObexOperation.Success)
            {
                return Errors.ErrorDeviceFileTransferSession.AddMetadata(
                    "error",
                    $"Unable to connect to the target OBEX service (response {response.Opcode})."
                );
            }

            OnConnected(response);

            return Errors.ErrorNone;
        }
        catch (Exception ex)
        {
            return Errors.ErrorUnexpected.AddMetadata("exception", ex.Message);
        }
    }

    protected virtual void OnConnected(ObexPacket connectionResponse) { }

    public virtual void Dispose()
    {
        try
        {
            Token.Release();
            Socket.Dispose();
        }
        catch
        {
            // ignored
        }
        finally
        {
            Sessions.RemoveSession(this);
            GC.SuppressFinalize(this);
        }
    }
}
