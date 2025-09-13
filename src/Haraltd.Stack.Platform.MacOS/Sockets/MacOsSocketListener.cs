using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.OutputStream;
using Haraltd.Stack.Base.Sockets;
using InTheHand.Net.Bluetooth;
using IOBluetooth;
using ObjCRuntime;

namespace Haraltd.Stack.Platform.MacOS.Sockets;

public class MacOsSocketListener(SocketListenerOptions options) : NSObject, ISocketListener
{
    private const string RfcommChannelOpenedSelector = "rfcommChannelOpenedNotification:channel:";

    private UserNotification _rfcommChannelNotification = null!;
    private bool _disposed;

    private SdpServiceRecord _sdpServiceRecord = null!;

    public event Action<ISocket>? OnConnected;

    public async ValueTask<ErrorData> StartAdvertisingAsync()
    {
        await ValueTask.CompletedTask;

        _sdpServiceRecord = SdpServiceRecord.PublishedServiceRecordWithDictionary(
            GetServiceDefinition()
        );
        if (_sdpServiceRecord == null)
            return Errors.ErrorUnexpected.AddMetadata("exception", "Service was not published");

        var channelResult = _sdpServiceRecord.GetRFCOMMChannelID(out var channelId);
        if (channelResult != 0 || channelId == 0)
        {
            return Errors.ErrorUnexpected.AddMetadata(
                "exception",
                "Service's channel ID was not found"
            );
        }

        _rfcommChannelNotification = RfcommChannel.RegisterForChannelOpenNotifications(
            this,
            new Selector(RfcommChannelOpenedSelector),
            channelId,
            UserNotificationChannelDirection.Incoming
        );
        if (_rfcommChannelNotification == null)
            return Errors.ErrorUnexpected.AddMetadata(
                "exception",
                "The RFCOMM channel was not registered"
            );

        return Errors.ErrorNone;
    }

    [Export(RfcommChannelOpenedSelector)]
    internal void OnRfcommChannelOpened(UserNotification notification, RfcommChannel channel)
    {
        if (!notification.Equals(_rfcommChannelNotification))
            return;

        try
        {
            var socket = new MacOsSocket(channel, options.ServiceUuid);
            OnConnected?.Invoke(socket);
        }
        catch (Exception e)
        {
            Output.Event(
                Errors.ErrorUnexpected.AddMetadata(
                    "exception",
                    $"Error on creating socket via listener: {e.Message}"
                ),
                OperationToken.None
            );
        }
    }

    private NSMutableDictionary<NSString, NSObject> GetServiceDefinition()
    {
        var serviceDefinition = new NSMutableDictionary<NSString, NSObject>();

        var serviceUuid = options.ServiceUuid;

        const int serviceNameId = (int)(100 + SdpAttributeIdentifierCode.ServiceName);
        var serviceNameKey = new NSString(serviceNameId.ToString());
        serviceDefinition[serviceNameKey] = new NSString(BluetoothService.GetName(serviceUuid));

        const int serviceIdentifierId = (int)SdpAttributeIdentifierCode.ServiceClassIDList;
        var serviceIdentifierKey = new NSString(serviceIdentifierId.ToString());
        serviceDefinition[serviceIdentifierKey] = NSArray.FromNSObjects(
            serviceUuid.ToShortestNativeUuid()
        );

        var protocolDefinition = NSArray.From([
            [SdpUuid.FromUuid16(SdpUuid16.L2Cap)],
            [
                SdpUuid.FromUuid16(SdpUuid16.Rfcomm),
                new NSDictionary(
                    new NSString("DataElementType"),
                    new NSNumber(1),
                    new NSString("DataElementSize"),
                    new NSNumber(1),
                    new NSString("DataElementValue"),
                    new NSNumber(options.ChannelId)
                ),
            ],
        ]);

        const int protocolDescriptorId = (int)SdpAttributeIdentifierCode.ProtocolDescriptorList;
        var protocolDescriptorKey = new NSString(protocolDescriptorId.ToString());
        serviceDefinition[protocolDescriptorKey] = protocolDefinition;

        return serviceDefinition;
    }

    public new void Dispose()
    {
        Dispose(true);
        base.Dispose();

        GC.SuppressFinalize(this);
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            _sdpServiceRecord?.RemoveServiceRecord();

            _rfcommChannelNotification?.Unregister();
        }
        catch
        {
            // ignored
        }

        if (disposing)
        {
            _rfcommChannelNotification?.Dispose();
            _rfcommChannelNotification = null!;

            _sdpServiceRecord?.Dispose();
            _sdpServiceRecord = null!;
        }

        base.Dispose(disposing);
    }
}
