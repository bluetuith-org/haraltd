using System.Collections.Concurrent;
using GoodTimeStudio.MyPhone.OBEX.Opp;
using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.Managers;
using Haraltd.Operations.OutputStream;
using Haraltd.Stack.Microsoft.Adapters;
using Haraltd.Stack.Microsoft.Windows;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using Windows.Devices.Bluetooth;
using static Haraltd.DataTypes.Events.IFileTransferEvent;

namespace Haraltd.Stack.Microsoft.Devices.Profiles;

internal static class Opp
{
    private static readonly OppSessions Sessions = new();

    private static readonly string TransferCacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "haraltd",
        "transfers"
    );

    internal static async Task<ErrorData> StartFileTransferSessionAsync(
        OperationToken token,
        string address
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

            return await Sessions.Start(token, address, new OppClientSession());
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
        string address,
        string filepath
    )
    {
        return !Sessions.GetClientSession(token, address, out var client, out var error)
            ? (null, error)
            : client.QueueFileSend(filepath);
    }

    internal static async Task<ErrorData> SendFileAsync(
        OperationToken token,
        string address,
        string filepath
    )
    {
        if (!Sessions.GetClientSession(token, address, out var client, out var error))
            return error;

        return await client.SendFileAsync(filepath);
    }

    internal static ErrorData CancelFileTransfer(OperationToken token, string address)
    {
        return Sessions.CancelTransfer(token, address);
    }

    internal static ErrorData StopFileTransferSession(OperationToken token, string address)
    {
        return Sessions.Stop(token, address);
    }

    internal static async Task<ErrorData> StartFileTransferServerAsync(
        OperationToken token,
        string destinationDirectory = ""
    )
    {
        if (string.IsNullOrEmpty(destinationDirectory))
            destinationDirectory = TransferCacheDir;

        if (!Path.Exists(destinationDirectory))
            Directory.CreateDirectory(destinationDirectory);

        var (adapter, error) = AdapterModelExt.ConvertToAdapterModel();
        if (error != Errors.ErrorNone)
            return error;

        if (
            Sessions.GetServerSession(token, adapter.Address, out var session, out _)
            && session.IsMainServer
        )
            return Errors.ErrorOperationInProgress.WrapError(
                new Dictionary<string, object>
                {
                    { "operation", "Object Push Server" },
                    { "exception", "An Object Push server instance is in progress" },
                }
            );

        return await Sessions.Start(
            token,
            adapter.Address,
            new OppServerSession(true, destinationDirectory)
        );
    }

    internal static ErrorData StopFileTransferServer(OperationToken token)
    {
        var (adapter, error) = AdapterModelExt.ConvertToAdapterModel();
        return error != Errors.ErrorNone ? error : Sessions.Stop(token, adapter.Address);
    }

    internal static bool IsServerSessionExist(
        OperationToken token,
        BluetoothAddress address,
        out OppServerSession session
    )
    {
        session = null;

        if (Sessions.GetSession(token, address, out OppServerSession oppSession))
            session = oppSession;

        return session != null;
    }
}

internal record OppSessions : IDisposable
{
    private readonly ConcurrentDictionary<BluetoothAddress, IOppSession> _sessions = [];

    public void Dispose()
    {
        foreach (var (_, s) in _sessions)
            s.Stop();
        _sessions.Clear();
    }

    internal async Task<ErrorData> Start(OperationToken token, string address, IOppSession session)
    {
        if (!GetBluetoothAddress(address, out var addr, out var error))
            return error;

        if (!_sessions.TryAdd(addr, session))
        {
            session.Dispose();
            return Errors.ErrorDeviceFileTransferSession.AddMetadata(
                "exception",
                "A session already exists with this address"
            );
        }

        session.Address = addr;
        session.Token = token;
        session.Sessions = this;

        return await session.Start();
    }

    internal ErrorData Stop(OperationToken token, string address)
    {
        if (!GetBluetoothAddress(address, out var addr, out var error))
            return error;

        if (!_sessions.TryGetValue(addr, out var session))
            return Errors.ErrorDeviceFileTransferSession.AddMetadata(
                "exception",
                "No session exists for Object Push"
            );

        if (session.Token.ClientId != token.ClientId)
            return Errors.ErrorDeviceFileTransferSession.AddMetadata(
                "exception",
                "This transfer is controlled by another client"
            );

        session.Stop();

        return Errors.ErrorNone;
    }

    internal ErrorData CancelTransfer(OperationToken token, string address)
    {
        if (!GetBluetoothAddress(address, out var addr, out var error))
            return error;

        if (!_sessions.TryGetValue(addr, out var session))
            return Errors.ErrorDeviceFileTransferSession.AddMetadata(
                "exception",
                "No session exists for Object Push"
            );

        if (
            (session is OppClientSession && session.Token.ClientId != token.ClientId)
            || session is OppServerSession
                && !AuthenticationManager.IsAgent(
                    token.ClientId,
                    AuthenticationManager.AuthAgentType.Obex
                )
        )
            return Errors.ErrorDeviceFileTransferSession.AddMetadata(
                "exception",
                "This transfer is controlled by another client"
            );

        session.CancelTransfer();

        return Errors.ErrorNone;
    }

    internal bool GetClientSession(
        OperationToken token,
        string address,
        out OppClientSession session,
        out ErrorData error
    )
    {
        return GetSession(token, address, out session, out error);
    }

    internal bool GetServerSession(
        OperationToken token,
        string address,
        out OppServerSession session,
        out ErrorData error
    )
    {
        return GetSession(token, address, out session, out error);
    }

    internal void RemoveSession(IOppSession session)
    {
        _sessions.TryRemove(session.Address, out _);
    }

    private bool GetSession<T>(
        OperationToken token,
        string address,
        out T session,
        out ErrorData error
    )
        where T : class, IOppSession
    {
        session = null;

        if (!GetBluetoothAddress(address, out var addr, out error))
            return false;

        if (!GetSession(token, addr, out session))
            error = Errors.ErrorDeviceFileTransferSession.AddMetadata(
                "exception",
                "No session exists for Object Push Client"
            );

        return session != null;
    }

    internal bool GetSession<T>(OperationToken token, BluetoothAddress addr, out T session)
        where T : class, IOppSession
    {
        session = null;

        foreach (var (_, s) in _sessions)
            if (
                s is T oppSession
                && oppSession.Address == addr
                && oppSession.Token.ClientId == token.ClientId
            )
            {
                session = oppSession;
                break;
            }

        return session != null;
    }

    internal IEnumerable<T> EnumerateSessions<T>()
        where T : class, IOppSession
    {
        foreach (var (_, s) in _sessions)
            if (s is T session)
                yield return session;
    }

    private static bool GetBluetoothAddress(
        string address,
        out BluetoothAddress addr,
        out ErrorData error
    )
    {
        addr = null;
        error = Errors.ErrorNone;

        try
        {
            addr = BluetoothAddress.Parse(address);
        }
        catch (Exception ex)
        {
            error = Errors.ErrorDeviceFileTransferSession.AddMetadata("exception", ex.Message);
        }

        return error == Errors.ErrorNone;
    }
}

internal interface IOppSession : IDisposable
{
    public BluetoothAddress Address { get; set; }
    public OperationToken Token { get; set; }

    public OppSessions Sessions { get; set; }

    public Task<ErrorData> Start();
    public void Stop();
    public void CancelTransfer();
}

internal sealed record OppServerSession(bool IsMainServer, string DestinationDirectory)
    : IOppSession
{
    public BluetoothAddress Address { get; set; }
    public OperationToken Token { get; set; }

    public OppSessions Sessions { get; set; }

    private Action _cancelFunc;
    private BluetoothOppServerSession _oppServer;

    public async Task<ErrorData> Start()
    {
        if (!IsMainServer)
            return Errors.ErrorNone;

        try
        {
            var server = new BluetoothOppServerSession(
                Token.LinkedCancelTokenSource,
                DestinationDirectory,
                data =>
                {
                    var fileTransferEvent = new FileTransferEventCombined()
                    {
                        Name = data.FileName,
                        FileName = data.FilePath,
                        Address = DeviceUtils.HostAddress(data.HostName),
                        FileSize = data.FileSize,
                        BytesTransferred = data.BytesTransferred,
                        Status = TransferStatus.Queued,
                    };

                    var tok = OperationManager.GenerateToken(0, Token.CancelTokenSource.Token);

                    return Output.ConfirmAuthentication(
                        new WindowsOppAuthEvent(10000, fileTransferEvent, tok),
                        AuthenticationManager.AuthAgentType.Obex
                    );
                }
            );
            await server.StartServerAsync();

            _oppServer = server;
            OperationManager.SetOperationProperties(Token);

            _oppServer.ClientAccepted += (_, args) =>
            {
                var address = DeviceUtils.HostAddress(args.ClientInfo.HostName.RawName);
                var fileTransferData = new FileTransferEventCombined();

                AddSubSession(address, () => args.ObexServer.StopServer()).Wait();

                args.ObexServer.ReceiveTransferEventHandler += (_, e) =>
                {
                    if (
                        !string.IsNullOrEmpty(e.FilePath)
                        && e.FilePath != fileTransferData.FileName
                    )
                    {
                        fileTransferData.Action = IEvent.EventAction.Added;
                    }
                    else
                    {
                        fileTransferData.Action = IEvent.EventAction.Updated;
                    }

                    fileTransferData.FileName = e.FilePath;
                    fileTransferData.Address = address;
                    fileTransferData.FileSize = e.FileSize;
                    fileTransferData.BytesTransferred = e.BytesTransferred;
                    fileTransferData.Status = TransferStatus.Active;

                    if (e.Error)
                    {
                        fileTransferData.Status = TransferStatus.Error;
                    }
                    else
                    {
                        if (e.Queued)
                            fileTransferData.Status = TransferStatus.Queued;

                        if (e.TransferDone || e.SessionClosed)
                            fileTransferData.Status = TransferStatus.Complete;
                    }

                    Output.Event(fileTransferData, Token);

                    if (e.TransferDone || e.SessionClosed)
                    {
                        fileTransferData.Action = IEvent.EventAction.Removed;
                        Output.Event(fileTransferData, Token);
                    }
                };
            };
            _oppServer.ClientDisconnected += (_, args) =>
            {
                RemoveSubSession(DeviceUtils.HostAddress(args.ClientInfo.HostName.RawName));
                args.ObexServer.ReceiveTransferEventHandler = null;

                if (args.ObexServerException != null)
                    Output.Event(
                        Errors.ErrorDeviceFileTransferSession.AddMetadata(
                            "exception",
                            args.ObexServerException.Message
                        ),
                        Token
                    );
            };
        }
        catch (Exception ex)
        {
            return Errors.ErrorDeviceFileTransferSession.AddMetadata("exception", ex.Message);
        }

        _ = Task.Run(() =>
        {
            Token.Wait();
            Stop();
        });

        return Errors.ErrorNone;
    }

    public void Stop()
    {
        CancelTransfer();
        Dispose();
    }

    public void CancelTransfer()
    {
        _cancelFunc?.Invoke();
    }

    public void Dispose()
    {
        Sessions?.RemoveSession(this);

        try
        {
            if (IsMainServer)
            {
                foreach (var session in Sessions?.EnumerateSessions<OppServerSession>()!)
                {
                    if (session.IsMainServer)
                        continue;

                    session.Stop();
                }

                _oppServer.Dispose();
                Token.Release();
            }
        }
        catch { }
    }

    private async Task AddSubSession(string address, Action cancelFunc)
    {
        if (!Sessions.GetServerSession(Token, address, out _, out _))
            await Sessions.Start(
                Token,
                address,
                new OppServerSession(false, DestinationDirectory) { _cancelFunc = cancelFunc }
            );
    }

    private void RemoveSubSession(string address)
    {
        if (Sessions.GetServerSession(Token, address, out var session, out _))
            session.Stop();
    }
}

internal sealed record OppClientSession : IOppSession
{
    private readonly BlockingCollection<string> _filequeue = [];

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private string AddressString => Address.ToString("C");

    public BluetoothAddress Address { get; set; }
    public OperationToken Token { get; set; }

    public OppSessions Sessions { get; set; }
    private BluetoothOppClientSession OppClient { get; set; }
    private BluetoothDevice _device;
    private bool _disposed;

    public async Task<ErrorData> Start()
    {
        try
        {
            _device = await DeviceUtils.GetBluetoothDeviceWithService(
                AddressString,
                BluetoothService.ObexObjectPush
            );

            var client = new BluetoothOppClientSession(_device, Token.LinkedCancelTokenSource);
            await client.ConnectAsync();
            if (client.ObexClient == null)
                throw new Exception("OPP ObexClient is null on connect");

            OppClient = client;
            OperationManager.SetOperationProperties(Token, true, true);

            var fileTransferData = new FileTransferEventCombined();

            client.ObexClient.TransferEventHandler += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Name) && e.Name != fileTransferData.Name)
                {
                    fileTransferData.Action = IEvent.EventAction.Added;
                }
                else
                {
                    fileTransferData.Action = IEvent.EventAction.Updated;
                }

                fileTransferData.Name = e.Name;
                fileTransferData.FileName = e.FileName;
                fileTransferData.Address = AddressString;
                fileTransferData.FileSize = e.FileSize;
                fileTransferData.BytesTransferred = e.BytesTransferred;
                fileTransferData.Status = TransferStatus.Active;

                if (e.Queued)
                    fileTransferData.Status = TransferStatus.Queued;

                if (e.Error)
                    fileTransferData.Status = TransferStatus.Error;
                else if (e.TransferDone)
                    fileTransferData.Status = TransferStatus.Complete;

                Output.Event(fileTransferData, Token);

                if (e.Error || e.TransferDone)
                {
                    fileTransferData.Action = IEvent.EventAction.Removed;
                    Output.Event(fileTransferData, Token);
                }
            };

            _device.ConnectionStatusChanged += OnDeviceOnConnectionStatusChanged;

            _ = Task.Run(RunTask);
            return Errors.ErrorNone;
        }
        catch (Exception ex)
        {
            return Errors.ErrorDeviceFileTransferSession.AddMetadata("exception", ex.Message);
        }

        async Task RunTask()
        {
            await FilesQueueProcessor(Token.CancelTokenSource.Token);

            if (OppClient.ObexClient != null)
                OppClient.ObexClient.TransferEventHandler = null;

            Stop();
        }
    }

    private void OnDeviceOnConnectionStatusChanged(BluetoothDevice _, object args)
    {
        Stop();
    }

    public void Stop()
    {
        CancelClientFileTransfer();
        Dispose();
    }

    public void CancelTransfer()
    {
        CancelClientFileTransfer();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            _semaphore.Wait();

            _filequeue?.CompleteAdding();
            OppClient?.Dispose();

            if (!Token.IsReleased())
                Token.Release();

            if (_device != null)
            {
                _device.ConnectionStatusChanged -= OnDeviceOnConnectionStatusChanged;
                _device.Dispose();
            }
        }
        catch { }
        finally
        {
            _semaphore.Release();

            _disposed = true;
            Sessions?.RemoveSession(this);
        }
    }

    public async Task<ErrorData> SendFileAsync(string filepath)
    {
        try
        {
            await _semaphore.WaitAsync();
            var task = OppClient.ObexClient?.SendFile(filepath);
            _semaphore.Release();

            await (task ?? Task.CompletedTask);
        }
        catch (Exception e)
        {
            Token.Release();
            return Errors.ErrorDeviceFileTransferSession.AddMetadata("exception", e.Message);
        }

        return Errors.ErrorNone;
    }

    public (FileTransferModel, ErrorData) QueueFileSend(string filepath)
    {
        long size;
        var fileTransferEvent = new FileTransferModel
        {
            Address = AddressString,
            Name = Path.GetFileName(filepath),
            Status = TransferStatus.Error,
        };

        try
        {
            var info = new FileInfo(filepath);
            size = info.Length;

            _filequeue.Add(filepath);
        }
        catch (Exception e)
        {
            return (
                fileTransferEvent,
                Errors.ErrorDeviceFileTransferSession.AddMetadata("exception", e.Message)
            );
        }

        return (
            fileTransferEvent with
            {
                FileName = filepath,
                Address = AddressString,
                Status = TransferStatus.Queued,
                FileSize = size,
            },
            Errors.ErrorNone
        );
    }

    private async Task FilesQueueProcessor(CancellationToken token)
    {
        try
        {
            foreach (var filepath in _filequeue.GetConsumingEnumerable(token))
            {
                if (OppClient?.ObexClient == null)
                    return;

                if (await SendFileAsync(filepath) is var error && error != Errors.ErrorNone)
                    Output.Event(error, Token);
            }
        }
        catch { }
    }

    private void CancelClientFileTransfer()
    {
        try
        {
            _semaphore.Wait();

            _filequeue?.CompleteAdding();
            OppClient?.ObexClient?.CancelTransfer();
        }
        catch { }
        finally
        {
            _semaphore.Release();
        }
    }
}
