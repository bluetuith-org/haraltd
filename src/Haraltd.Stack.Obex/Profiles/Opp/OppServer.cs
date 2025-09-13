using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.Generic;
using Haraltd.Operations.OutputStream;
using Haraltd.Stack.Obex.Headers;
using Haraltd.Stack.Obex.Packet;
using Haraltd.Stack.Obex.Session;

namespace Haraltd.Stack.Obex.Profiles.Opp;

public record OppServerProperties : ObexServerProperties
{
    public readonly Func<FileTransferEventCombined, bool> AuthFunc = static delegate
    {
        return true;
    };
    public readonly string DestinationDirectory = "";

    public OppServerProperties() { }

    public OppServerProperties(
        string destinationDirectory,
        Func<FileTransferEventCombined, bool> authFunc
    )
    {
        AuthFunc = authFunc;
        DestinationDirectory = Directory.Exists(destinationDirectory)
            ? destinationDirectory
            : Path.GetTempPath();
    }
}

public class OppServer(OppServerProperties properties)
    : ObexServer<OppSubServer, OppServerProperties>(OppStatic.Id, properties);

public class OppSubServer : ObexSubServer<OppServerProperties>
{
    public override async ValueTask<ErrorData> StartAsync()
    {
        var error = await base.StartAsync();

        return error == Errors.ErrorNone ? await RunFileTransferAsync() : error;
    }

    private async ValueTask<ErrorData> RunFileTransferAsync()
    {
        var data = NewData();
        // ReSharper disable once MoveLocalFunctionAfterJumpStatement
        FileTransferEventCombined NewData()
        {
            return new FileTransferEventCombined(true)
            {
                Address = Socket.Address.ToString("C"),
                Status = IFileTransferEvent.TransferStatus.Active,
                Action = IEvent.EventAction.Added,
            };
        }

        FileStream file = null;

        var firstPut = true;
        var finalPut = false;
        var aborted = false;
        var disconnected = false;

        var exception = false;

        try
        {
            Cts.Token.ThrowIfCancellationRequested();

            ObexPacket connectPacket = await ObexPacket.ReadFromStream<ObexConnectPacket>(
                Socket.Reader
            );
            switch (connectPacket.Opcode.Operation)
            {
                case ObexOperation.Connect:
                    connectPacket.Opcode = new ObexOpcode(ObexOperation.Success, true);

                    connectPacket.WriteToStream(Socket.Writer);
                    await Socket.Writer.StoreAsync();

                    break;

                default:
                    connectPacket = new ObexPacket(
                        new ObexOpcode(ObexOperation.ServiceUnavailable, true)
                    );

                    connectPacket.WriteToStream(Socket.Writer);
                    await Socket.Writer.StoreAsync();

                    throw new ObexException("Unknown OpCode " + connectPacket.Opcode.Operation);
            }

            Cts.Token.ThrowIfCancellationRequested();

            while (!disconnected && !aborted && !Cts.Token.IsCancellationRequested)
            {
                if (finalPut)
                {
                    var src = file.Name;
                    await file.DisposeAsync();

                    MoveFile(src, data.FileName);
                    Output.Event(data, Token);

                    data.Action = IEvent.EventAction.Removed;
                    Output.Event(data with { Action = IEvent.EventAction.Removed }, Token);

                    file = null!;
                }

                file ??= File.OpenWrite(
                    Path.Combine(Properties.DestinationDirectory, Path.GetRandomFileName())
                );

                var sendPacket = new ObexPacket(new ObexOpcode(ObexOperation.Success, true));

                var packet = await ObexPacket.ReadFromStream(Socket.Reader);
                switch (packet.Opcode.Operation)
                {
                    case ObexOperation.Put:
                        if (finalPut)
                            data = NewData();

                        var (name, fileSize, isFinal, buffer) = GetPacketInfo(packet, firstPut);
                        if (fileSize > 0)
                            data.FileSize = fileSize;

                        if (!string.IsNullOrEmpty(name))
                        {
                            data.Name = name;
                            data.FileName = Path.Combine(Properties.DestinationDirectory, name);
                        }

                        if (firstPut)
                        {
                            Output.Event(
                                data with
                                {
                                    Status = IFileTransferEvent.TransferStatus.Queued,
                                },
                                Token
                            );

                            if (!Properties.AuthFunc(data))
                            {
                                throw new UnauthorizedAccessException("Access denied");
                            }

                            data.Action = IEvent.EventAction.Updated;
                        }

                        firstPut = false;
                        finalPut = isFinal;
                        if (!packet.Opcode.IsFinalBitSet)
                            sendPacket = new ObexPacket(
                                new ObexOpcode(ObexOperation.Continue, true)
                            );

                        file.Write(buffer);

                        sendPacket.WriteToStream(Socket.Writer);
                        await Socket.Writer.StoreAsync();
                        data.BytesTransferred += buffer.Length;

                        if (finalPut)
                        {
                            data.Status = IFileTransferEvent.TransferStatus.Complete;
                            continue;
                        }

                        Output.Event(data, Token);
                        data.Action = IEvent.EventAction.Updated;

                        continue;

                    case ObexOperation.Abort:
                    case ObexOperation.Disconnect:
                        disconnected = packet.Opcode.Operation == ObexOperation.Disconnect;
                        aborted = packet.Opcode.Operation == ObexOperation.Abort;

                        sendPacket.WriteToStream(Socket.Writer);
                        await Socket.Writer.StoreAsync();

                        break;

                    default:
                        throw new ObexException(
                            $"Got unexpected operation code during transfer: {packet.Opcode.Operation}"
                        );
                }
            }
        }
        catch (Exception ex)
        {
            exception = true;
            return Errors.ErrorDeviceFileTransferSession.AddMetadata("exception", ex.Message);
        }
        finally
        {
            if ((!disconnected && exception) || Cts.IsCancellationRequested || aborted)
            {
                data.Status = IFileTransferEvent.TransferStatus.Error;
                Output.Event(data, Token);

                data.Action = IEvent.EventAction.Removed;
                Output.Event(data, Token);
            }

            try
            {
                if (file != null)
                {
                    var name = file.Name;

                    await file.DisposeAsync();
                    DeleteTempFile(name);
                }

                if (Cts.IsCancellationRequested && !aborted && !disconnected)
                {
                    var packet = new ObexPacket(new ObexOpcode(ObexOperation.Abort, true));
                    packet.WriteToStream(Socket.Writer);

                    await Socket.Writer.StoreAsync();
                }
            }
            catch
            {
                // ignore
            }
            finally
            {
                Dispose();
            }
        }

        return Errors.ErrorNone;
    }

    private static void DeleteTempFile(string file)
    {
        if (string.IsNullOrEmpty(file))
            return;

        if (File.Exists(file))
            File.Delete(file);
    }

    private static void MoveFile(string src, string dest)
    {
        if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(dest) || src == dest)
            return;

        new FileInfo(src).MoveTo(dest, true);
    }

    private static (string FileName, int FileSize, bool IsFinal, byte[] buffer) GetPacketInfo(
        ObexPacket packet,
        bool isFirstPut
    )
    {
        var actualFileName = "";
        var fileSize = 0;

        var headerId = packet.Opcode.IsFinalBitSet ? HeaderId.EndOfBody : HeaderId.Body;

        if (packet.Headers.TryGetValue(HeaderId.Name, out var nameHeader))
            actualFileName = nameHeader.GetValueAsUnicodeString(true);

        if (packet.Headers.TryGetValue(HeaderId.Length, out var lengthHeader))
            fileSize = lengthHeader.GetValueAsInt32();

        if (isFirstPut)
        {
            if (string.IsNullOrEmpty(actualFileName) || fileSize <= 0)
                throw new Exception(
                    "Invalid first packet from remote device. Filename is empty and/or file size is zero."
                );
        }

        return (
            actualFileName,
            fileSize,
            packet.Opcode.IsFinalBitSet,
            packet.GetHeader(headerId).Buffer
        );
    }
}
