using Haraltd.DataTypes.Generic;
using Haraltd.Stack.Base.Sockets;
using InTheHand.Net.Sockets;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;

namespace Haraltd.Stack.Microsoft.Sockets;

public partial class WindowsSocketListener(Guid serviceUuid) : ISocketListener
{
    private readonly StreamSocketListener _socketListener = new();
    private RfcommServiceProvider _serviceProvider;

    public event Action<ISocket> OnConnected;

    public async ValueTask<ErrorData> StartAdvertisingAsync()
    {
        try
        {
            _socketListener.ConnectionReceived += SocketListenerOnConnectionReceived;
            _serviceProvider = await RfcommServiceProvider.CreateAsync(
                RfcommServiceId.FromUuid(serviceUuid)
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
        OnConnected?.Invoke(new WindowsSocket(args.Socket, serviceUuid));
    }

    public void Dispose()
    {
        if (_socketListener != null)
            _socketListener.ConnectionReceived -= SocketListenerOnConnectionReceived;

        _socketListener?.Dispose();
    }
}
