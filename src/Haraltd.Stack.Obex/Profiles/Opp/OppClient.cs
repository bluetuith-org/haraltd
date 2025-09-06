using System.Collections.Concurrent;
using System.Text;
using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.Operations.OutputStream;
using Haraltd.Stack.Obex.Headers;
using Haraltd.Stack.Obex.Packet;
using Haraltd.Stack.Obex.Session;

namespace Haraltd.Stack.Obex.Profiles.Opp;

public record OppClientProperties : ObexClientProperties;

public class OppClient(OppClientProperties properties)
    : ObexClient<OppClientProperties>(OppService.Id, properties)
{
    private ObexHeader _connectionIdHeader;
    private ushort _clientPacketSize = 256;
    private readonly BlockingCollection<string> _fileQueue = [];

    public override async ValueTask<ErrorData> StartAsync()
    {
        var error = await base.StartAsync();
        if (error == Errors.ErrorNone)
        {
            _ = Task.Run(async () => await FilesQueueProcessorAsync(Token.CancelTokenSource.Token));
        }

        return error;
    }

    public override async ValueTask<ErrorData> StopAsync()
    {
        Dispose();
        return await ValueTask.FromResult(Errors.ErrorNone);
    }

    public (FileTransferModel, ErrorData) QueueFileSend(string filepath)
    {
        var (fileTransfer, error) = GetInfo(filepath);
        if (error == Errors.ErrorNone)
        {
            try
            {
                _fileQueue.Add(fileTransfer.FileName);
            }
            catch (Exception e)
            {
                error = Errors.ErrorDeviceFileTransferSession.AddMetadata("exception", e.Message);
            }
        }

        return (fileTransfer, error);
    }

    protected override void OnConnected(ObexPacket connectionResponse)
    {
        _connectionIdHeader = connectionResponse.GetHeader(HeaderId.ConnectionId);

        _clientPacketSize = Math.Max(
            _clientPacketSize,
            (ushort)(
                ((ObexConnectPacket)connectionResponse).MaximumPacketLength - _clientPacketSize
            )
        );
    }

    private (FileTransferModel, ErrorData) GetInfo(string filepath)
    {
        var fileTransfer = new FileTransferModel
        {
            Address = Socket.Address.ToString("C"),
            Name = Path.GetFileName(filepath),
            Status = IFileTransferEvent.TransferStatus.Error,
        };

        try
        {
            var info = new FileInfo(filepath);

            fileTransfer.FileSize = info.Length;
            fileTransfer.Name = info.Name;
            fileTransfer.FileName = info.FullName;
            fileTransfer.Status = IFileTransferEvent.TransferStatus.Queued;
        }
        catch (Exception e)
        {
            return (
                fileTransfer,
                Errors.ErrorDeviceFileTransferSession.AddMetadata("exception", e.Message)
            );
        }

        return (fileTransfer, Errors.ErrorNone);
    }

    private async Task FilesQueueProcessorAsync(CancellationToken token)
    {
        try
        {
            foreach (var fileName in _fileQueue.GetConsumingEnumerable(token))
            {
                if (await SendFileAsync(fileName) is var error && error != Errors.ErrorNone)
                {
                    Output.Event(error, Token);
                    await StopAsync();

                    return;
                }
            }
        }
        catch
        {
            // ignore
        }
    }

    private async ValueTask<ErrorData> SendFileAsync(string fileName)
    {
        var file = new FileInfo(fileName);
        var filename = Path.GetFileName(file.Name);

        await using FileStream fileStream = new(
            fileName,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            _clientPacketSize,
            true
        );

        var buffer = new byte[_clientPacketSize];
        var bodyHeader = new ObexHeader(HeaderId.Body, buffer);
        var eobHeader = new ObexHeader(HeaderId.EndOfBody, buffer);

        var data = new FileTransferEventCombined(false)
        {
            Address = Socket.Address.ToString("C"),
            Name = filename,
            FileName = fileName,
            FileSize = file.Length,
            Action = IEvent.EventAction.Updated,
            Status = IFileTransferEvent.TransferStatus.Active,
        };

        var request = new ObexPacket(
            new ObexOpcode(ObexOperation.Put, false),
            _connectionIdHeader!,
            new ObexHeader(HeaderId.Name, filename, true, Encoding.BigEndianUnicode),
            new ObexHeader(HeaderId.Length, (int)file.Length),
            new ObexHeader(HeaderId.Type, MimeTypes.GetMimeType(file.Name), true, Encoding.ASCII),
            bodyHeader
        );

        var completed = false;
        var packetModified = false;
        var firstPut = true;

        try
        {
            int bytesRead;
            var totalRead = 0;

            while ((bytesRead = fileStream.Read(buffer, 0, _clientPacketSize)) > 0)
            {
                totalRead += bytesRead;

                if (totalRead > 0 && !firstPut && !packetModified)
                {
                    request = new ObexPacket(new ObexOpcode(ObexOperation.Put, false), bodyHeader);

                    data.Name = data.FileName = "";
                    packetModified = true;
                }

                if (Cts.IsCancellationRequested)
                {
                    await AbortAsync();

                    return Errors.ErrorOperationCancelled;
                }

                if (bytesRead != _clientPacketSize && totalRead == file.Length)
                {
                    eobHeader.Buffer = buffer[..bytesRead];

                    request.Opcode = new ObexOpcode(ObexOperation.Put, true);
                    request.ReplaceHeader(HeaderId.Body, eobHeader);
                }

                request.WriteToStream(Socket.Writer);
                await Socket.Writer.StoreAsync();

                var subResponse = await ObexPacket.ReadFromStream(Socket.Reader);
                switch (subResponse.Opcode.Operation)
                {
                    case ObexOperation.Success:
                        completed = true;
                        break;

                    case ObexOperation.Continue:
                        break;

                    default:
                        throw new ObexException($"Operation code: {subResponse.Opcode.Operation}");
                }

                if (firstPut && data.BytesTransferred == 0)
                    Output.Event(
                        data with
                        {
                            Status = IFileTransferEvent.TransferStatus.Queued,
                            Action = IEvent.EventAction.Added,
                        },
                        Token
                    );

                data.BytesTransferred = totalRead;
                Output.Event(data, Token);

                firstPut = false;
            }
        }
        catch (Exception ex)
        {
            return Errors.ErrorDeviceFileTransferSession.AddMetadata("exception", ex.Message);
        }
        finally
        {
            data.Status = !completed
                ? IFileTransferEvent.TransferStatus.Error
                : IFileTransferEvent.TransferStatus.Complete;
            Output.Event(data, Token);
            Output.Event(data with { Action = IEvent.EventAction.Removed }, Token);
        }

        return Errors.ErrorNone;
    }

    private async Task AbortAsync()
    {
        if (!Cts.IsCancellationRequested)
            await Cts.CancelAsync();

        try
        {
            var request = new ObexPacket(new ObexOpcode(ObexOperation.Abort, true));
            request.WriteToStream(Socket.Writer);
            await Socket.Writer.StoreAsync();
        }
        catch
        {
            //ignore
        }
    }

    public override void Dispose()
    {
        _fileQueue.CompleteAdding();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
