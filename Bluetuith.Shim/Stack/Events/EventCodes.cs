using Bluetuith.Shim.Types;

namespace Bluetuith.Shim.Stack.Events;

public record class StackEventCode : EventCode
{
    public enum EventCodeValue : int
    {
        DeviceConnection = 1,
        DeviceDiscovery,
        DevicePairingAuthentication,
        ServerConnection,
        FileTransferClient,
        FileTransferServer,
        PhonebookAccessClient,
        MessageAccessClient,
        MessageAccessServer,
        A2dpClient,
    }

    public static EventCode DeviceConnectionStatusEventCode = new StackEventCode(EventCodeValue.DeviceConnection);
    public static EventCode DeviceDiscoveryStatusEventCode = new StackEventCode(EventCodeValue.DeviceDiscovery);
    public static EventCode DevicePairingAuthStatusEventCode = new StackEventCode(EventCodeValue.DevicePairingAuthentication);
    public static EventCode ServerConnectionStatusEventCode = new StackEventCode(EventCodeValue.ServerConnection);
    public static EventCode FileTransferClientEventCode = new StackEventCode(EventCodeValue.FileTransferClient);
    public static EventCode FileTransferServerEventCode = new StackEventCode(EventCodeValue.FileTransferServer);
    public static EventCode PhonebookAccessClientEventCode = new StackEventCode(EventCodeValue.PhonebookAccessClient);
    public static EventCode MessageAccessClientEventCode = new StackEventCode(EventCodeValue.MessageAccessClient);
    public static EventCode MessageAccessServerEventCode = new StackEventCode(EventCodeValue.MessageAccessServer);
    public static EventCode A2dpClientEventCode = new StackEventCode(EventCodeValue.A2dpClient);

    public StackEventCode(EventCodeValue value) : base(value.ToString(), (int)value) { }
}
