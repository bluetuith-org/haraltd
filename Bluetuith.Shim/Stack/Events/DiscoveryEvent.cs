using Bluetuith.Shim.Stack.Models;
using Bluetuith.Shim.Types;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bluetuith.Shim.Stack.Events;

public record class DiscoveryEvent : Event
{
    public enum DiscoveryStatus
    {
        Started = 0,
        InProgress,
        Stopped
    }

    public enum DeviceInfoStatus
    {
        Added = 0,
        Removed,
        Updated
    }

    public DiscoveryStatus Status;
    public DeviceInfoStatus DeviceStatus;
    public DeviceModel Device;

    public DiscoveryEvent(DiscoveryStatus status)
    {
        EventType = StackEventCode.DeviceDiscoveryStatusEventCode;
        Status = status;
        Device = new();
    }

    public override string ToConsoleString()
    {
        StringBuilder stringBuilder = new();

        if (Device.Address != "")
        {
            stringBuilder.AppendLine($"[+] Device {DeviceStatus}:");
            stringBuilder.Append(Device.ToConsoleString());
            stringBuilder.AppendLine();
        }
        else
        {
            stringBuilder.AppendLine($"[+] Discovery status: {Status}");
        }

        return stringBuilder.ToString();
    }

    public override JsonObject ToJsonObject()
    {
        return new JsonObject()
        {
            ["discoveryEvent"] = JsonSerializer.SerializeToNode(
                new JsonObject()
                {
                    ["status"] = Status.ToString(),
                    ["deviceInfoStatus"] = DeviceStatus.ToString(),
                    ["discoveredDevice"] = JsonSerializer.SerializeToNode(Device.ToJsonObject())
                }
            )
        };
    }
}
