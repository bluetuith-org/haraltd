using Haraltd.DataTypes.Generic;
using Haraltd.Stack.Base.Sockets;
using InTheHand.Net;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;

namespace Haraltd.Stack.Platform.Windows.Sockets;

public partial class WindowsSocket : ISocket
{
    private StreamSocket _socket;
    private readonly Guid _serviceUuid;
    private readonly bool _isConnected;

    private BluetoothDevice _device;

    private bool _disposed;

    private BluetoothDevice Device
    {
        get
        {
            if (_device != null)
                return _device;

            try
            {
                _device = BluetoothDevice
                    .FromBluetoothAddressAsync(Address)
                    .GetAwaiter()
                    .GetResult();
                _device.ConnectionStatusChanged += DeviceOnConnectionStatusChanged;
            }
            catch
            {
                _device = null;
            }

            return _device;
        }
        set => _device = value;
    }

    public int Mtu => 0;
    public event Action<bool> ConnectionStatusEvent;
    public bool CanSubscribeToEvents => Device != null;

    public BluetoothAddress Address { get; }
    public ISocketStreamReader Reader { get; private set; }
    public ISocketStreamWriter Writer { get; private set; }

    public WindowsSocket(SocketOptions options)
    {
        Address = options.DeviceAddress;
        _serviceUuid = options.ServiceUuid;
        _socket = new StreamSocket();
    }

    public WindowsSocket(StreamSocket socket, Guid serviceUuid)
    {
        _socket = socket;
        _serviceUuid = serviceUuid;

        if (
            BluetoothAddress.TryParse(
                socket
                    .Information.RemoteAddress.RawName.Replace("(", string.Empty)
                    .Replace(")", string.Empty),
                out var address
            )
        )
            Address = address;

        Reader = new WindowsSocketStreamReader(_socket.InputStream);
        Writer = new WindowsSocketStreamWriter(_socket.OutputStream);

        _isConnected = true;
    }

    public async Task<ErrorData> ConnectAsync()
    {
        if (_isConnected)
            return Errors.ErrorDeviceAlreadyConnected;

        try
        {
            _device = Device;
            if (_device == null)
                return Errors.ErrorDeviceNotFound;

            var services = await _device.GetRfcommServicesAsync();
            if (services == null || services.Services.Count == 0)
                return Errors.ErrorDeviceServicesNotFound;

            var service = services.Services.FirstOrDefault(s => s.ServiceId.Uuid == _serviceUuid);
            if (service == null)
                return Errors.ErrorDeviceServicesNotFound;

            var access = await service.RequestAccessAsync();
            if (access != DeviceAccessStatus.Allowed)
                return Errors.ErrorUnexpected.AddMetadata("access", "Access was denied");

            await _socket.ConnectAsync(
                service.ConnectionHostName,
                service.ConnectionServiceName,
                SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication
            );

            Reader = new WindowsSocketStreamReader(_socket.InputStream);
            Writer = new WindowsSocketStreamWriter(_socket.OutputStream);
        }
        catch (Exception ex)
        {
            return Errors.ErrorUnexpected.AddMetadata("exception", ex.Message);
        }

        return Errors.ErrorNone;
    }

    private void DeviceOnConnectionStatusChanged(BluetoothDevice sender, object args)
    {
        ConnectionStatusEvent?.Invoke(
            sender.ConnectionStatus == BluetoothConnectionStatus.Connected
        );
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

        if (Device != null)
            Device.ConnectionStatusChanged -= DeviceOnConnectionStatusChanged;

        if (disposing)
        {
            Device?.Dispose();
            Device = null!;

            Writer?.Dispose();
            Writer = null!;

            Reader?.Dispose();
            Reader = null;

            _socket?.Dispose();
            _socket = null!;
        }
    }
}
