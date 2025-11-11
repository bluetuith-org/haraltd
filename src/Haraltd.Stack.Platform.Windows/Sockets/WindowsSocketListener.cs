using Haraltd.DataTypes.Generic;
using Haraltd.Stack.Base.Sockets;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;

namespace Haraltd.Stack.Platform.Windows.Sockets;

public partial class WindowsSocketListener(SocketListenerOptions options) : ISocketListener
{
    private StreamSocketListener _socketListener = new();
    private RfcommServiceProvider _serviceProvider;

    private bool _disposed;

    public event Action<ISocket> OnConnected;

    public async ValueTask<ErrorData> StartAdvertisingAsync()
    {
        try
        {
            _socketListener.ConnectionReceived += SocketListenerOnConnectionReceived;
            _serviceProvider = await RfcommServiceProvider.CreateAsync(
                RfcommServiceId.FromUuid(options.ServiceUuid)
            );

            await _socketListener.BindServiceNameAsync(
                _serviceProvider.ServiceId.AsString(),
                SocketProtectionLevel.BluetoothEncryptionWithAuthentication
            );
            _serviceProvider.StartAdvertising(_socketListener);
        }
        catch (Exception ex)
        {
            Dispose();
            return Errors.ErrorUnexpected.AddMetadata("exception", ex.Message);
        }

        return Errors.ErrorNone;
    }

    private void SocketListenerOnConnectionReceived(
        StreamSocketListener sender,
        StreamSocketListenerConnectionReceivedEventArgs args
    )
    {
        OnConnected?.Invoke(new WindowsSocket(args.Socket, options.ServiceUuid));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        _disposed = true;

        _serviceProvider?.StopAdvertising();
        _serviceProvider = null;

        if (_socketListener != null)
            _socketListener.ConnectionReceived -= SocketListenerOnConnectionReceived;

        if (disposing)
        {
            _socketListener?.Dispose();
            _socketListener = null;
        }
    }
}
