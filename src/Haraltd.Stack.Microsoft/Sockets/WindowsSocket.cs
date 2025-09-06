using Haraltd.DataTypes.Generic;
using Haraltd.Stack.Base.Sockets;
using InTheHand.Net;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;

namespace Haraltd.Stack.Microsoft.Sockets;

public partial class WindowsSocket : ISocket
{
    private readonly StreamSocket _socket;
    private readonly Guid _serviceUuid;
    private readonly bool _isConnected;

    public BluetoothAddress Address { get; }
    public ISocketStreamReader Reader { get; private set; }
    public ISocketStreamWriter Writer { get; private set; }

    public WindowsSocket(BluetoothAddress address, Guid serviceUuid)
    {
        Address = address;
        _serviceUuid = serviceUuid;
        _socket = new StreamSocket();
    }

    public WindowsSocket(StreamSocket socket, Guid serviceUuid)
    {
        _socket = socket;
        _serviceUuid = serviceUuid;

        Address = BluetoothAddress.Parse(
            socket
                .Information.RemoteAddress.RawName.Replace("(", string.Empty)
                .Replace(")", string.Empty)
        );

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
            var device = await BluetoothDevice.FromBluetoothAddressAsync(Address);
            if (device == null)
                return Errors.ErrorDeviceNotFound;

            var services = await device.GetRfcommServicesAsync();
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

    public void Dispose()
    {
        Writer.Dispose();
        Reader.Dispose();

        _socket.Dispose();

        GC.SuppressFinalize(this);
    }
}
