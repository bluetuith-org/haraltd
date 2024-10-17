using Bluetuith.Shim.Types;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bluetuith.Shim.Stack.Events;

public record class ConnectionStatusEvent : Event
{
    public enum ConnectionStatus : int
    {
        DeviceConnecting = 1,
        DeviceConnected,
        DeviceDisconnected,
        ServerStarting,
        ServerStarted,
        ServerStopped,
    }

    private readonly HostEvent _hostStatus;

    public ConnectionStatus State { get; set; }
    public EventCode RequestedByEvent { get; set; }

    public ConnectionStatusEvent(HostEvent hostStatus, EventCode eventCode, EventCode requestedBy)
    {
        _hostStatus = hostStatus;
        EventType = eventCode;
        RequestedByEvent = requestedBy;
    }

    public override string ToConsoleString()
    {
        var connectionStatus = "";
        switch (State)
        {
            case ConnectionStatus.DeviceConnected:
            case ConnectionStatus.ServerStarted:
                connectionStatus = "Connected to";
                break;
            case ConnectionStatus.DeviceDisconnected:
            case ConnectionStatus.ServerStopped:
                connectionStatus = "Disconnected from";
                break;
            case ConnectionStatus.DeviceConnecting:
            case ConnectionStatus.ServerStarting:
                connectionStatus = "Connecting to";
                break;
        }

        return $"[+] {connectionStatus} {_hostStatus.Name} for {RequestedByEvent.Name}{Environment.NewLine}";
    }

    public override JsonObject ToJsonObject()
    {
        return new JsonObject()
        {
            ["connectionEvent"] = JsonSerializer.SerializeToNode(
                new JsonObject()
                {
                    ["hostType"] = _hostStatus.Host.ToString(),
                    ["hostName"] = _hostStatus.Name,
                    ["connectionState"] = State.ToString(),
                    ["requesterEventId"] = RequestedByEvent.Value,
                    ["requesterEventName"] = RequestedByEvent.Name,
                }
            )
        };
    }
}

public record class DeviceConnectionStatusEvent : ConnectionStatusEvent
{
    public DeviceConnectionStatusEvent(string address, EventCode requestedBy) :
        base(new HostEvent(address, HostEvent.HostType.Device), StackEventCode.DeviceConnectionStatusEventCode, requestedBy)
    { }
}

public record class ServerConnectionStatusEvent : ConnectionStatusEvent
{
    public ServerConnectionStatusEvent(string serverName, EventCode requestedBy) :
        base(new HostEvent(serverName, HostEvent.HostType.Server), StackEventCode.ServerConnectionStatusEventCode, requestedBy)
    { }
}

public record class HostEvent
{
    public enum HostType : int
    {
        Device = 0,
        Server,
    }

    public string Name;
    public HostType Host;

    public HostEvent(string name, HostType host)
    {
        Name = name;
        Host = host;
    }
}