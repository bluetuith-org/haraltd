using System.IO.Pipelines;
using DotNext.Threading.Tasks;
using Haraltd.DataTypes.Generic;
using Haraltd.Stack.Base.Sockets;
using Haraltd.Stack.Platform.MacOS.Devices;
using InTheHand.Net;
using IOBluetooth;

namespace Haraltd.Stack.Platform.MacOS.Sockets;

public class MacOsSocket : RfcommChannelDelegate, ISocket
{
    private readonly SocketOptions _options;

    private RfcommChannel _rfcommChannel;
    private Pipe _inputPipe = new();
    private readonly TaskCompletionSource<bool> _socketOpenComplete = new();

    private bool _disposed;

    private Guid Service => _options.ServiceUuid;
    public BluetoothAddress Address => _options.DeviceAddress;

    public int Mtu { get; private set; } = 256;

    private MacOsStreamReader? _reader;
    public ISocketStreamReader Reader =>
        _reader ?? throw new NullReferenceException("Reader is not initialized");

    private MacOsStreamWriter? _writer;
    public ISocketStreamWriter Writer =>
        _writer ?? throw new NullReferenceException("Writer is not initialized");

    public bool CanSubscribeToEvents => Reader != null && Writer != null;
    public event Action<bool>? ConnectionStatusEvent;

    public MacOsSocket(SocketOptions options)
    {
        _options = options;
        _rfcommChannel = null!;
    }

    internal MacOsSocket(RfcommChannel channel, Guid serviceUuid)
    {
        ArgumentNullException.ThrowIfNull(channel);

        var device = channel.Device;
        if (device is not { IsPaired: true, IsConnected: true })
            throw new ArgumentException($"{nameof(device)} is not connected");

        _options = new SocketOptions
        {
            DeviceAddress = device.FormattedAddress,
            ServiceUuid = serviceUuid,
        };

        _rfcommChannel = channel;
        _reader = new MacOsStreamReader(_inputPipe.Reader);
        _writer = new MacOsStreamWriter(_rfcommChannel);
        _rfcommChannel.Delegate = this;

        Mtu = channel.Mtu < 256 ? 256 : channel.Mtu;
    }

    public async Task<ErrorData> ConnectAsync()
    {
        var nativeUuid = Service.ToNativeUuid();

        var device = BluetoothDevice.DeviceFromBluetoothAddress(Address);
        if (device == null)
            return Errors.ErrorDeviceNotFound;

        if (!device.IsPaired)
            return Errors.ErrorDeviceNotPaired;

        if (!device.IsConnected)
        {
            var callBack = new ConnectionAsyncCallBack();
            var conn = device.OpenConnection(callBack);

            if (conn != 0 || !await callBack.GetConnectionStatus())
                return Errors.ErrorDeviceNotConnected;
        }

        var serviceRecord = device.GetServiceRecordForUuid(nativeUuid);
        if (serviceRecord == null)
        {
            return Errors.ErrorDeviceNotConnected.AddMetadata(
                "exception",
                "This service does not exist"
            );
        }

        var ret = serviceRecord.GetRFCOMMChannelID(out var channelId);
        if (ret != 0)
        {
            return Errors.ErrorDeviceNotConnected.AddMetadata(
                "exception",
                "Cannot get the service's RFCOMM channel"
            );
        }

        ret = device.OpenRfcommChannelAsync(out _rfcommChannel, channelId, this);
        if (ret != 0 || !await _socketOpenComplete.Task)
        {
            return Errors.ErrorDeviceNotConnected.AddMetadata(
                "exception",
                "Could not open RFCOMM channel"
            );
        }

        Mtu = _rfcommChannel.Mtu;
        _reader = new MacOsStreamReader(_inputPipe.Reader);
        _writer = new MacOsStreamWriter(_rfcommChannel);

        return Errors.ErrorNone;
    }

    private void ConnectionStatus(bool connected)
    {
        ConnectionStatusEvent?.Invoke(connected);
    }

    public override void RfcommChannelOpenComplete(RfcommChannel rfcommChannel, int error)
    {
        if (!_socketOpenComplete.Task.IsCompleted)
            _socketOpenComplete.TrySetResult(error == 0);
    }

    public override void RfcommChannelClosed(RfcommChannel rfcommChannel)
    {
        Dispose(true);
    }

    public override void RfcommChannelData(
        RfcommChannel rfcommChannel,
        IntPtr dataPointer,
        UIntPtr dataLength
    )
    {
        try
        {
            unsafe
            {
                var span = new Span<byte>(dataPointer.ToPointer(), (int)dataLength);

                var memory = _inputPipe.Writer.GetSpan(span.Length);
                span.CopyTo(memory);

                _inputPipe.Writer.Advance(span.Length);

                _inputPipe.Writer.FlushAsync().Wait();
            }
        }
        catch
        {
            Dispose(true);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            _rfcommChannel?.CloseChannel();
        }
        catch
        {
            // ignored
        }

        if (disposing)
        {
            ConnectionStatus(false);
            ConnectionStatusEvent = null;

            _inputPipe?.Reader.Complete();
            _inputPipe?.Writer.Complete();
            _inputPipe = null!;

            _rfcommChannel?.Dispose();
            _rfcommChannel = null!;

            _reader?.Dispose();
            _reader = null;

            _writer?.Dispose();
            _writer = null;
        }

        base.Dispose(disposing);
    }
}
