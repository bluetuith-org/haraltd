using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.Serializer;
using InTheHand.Net.Bluetooth;
using static Haraltd.DataTypes.Generic.IEvent;

namespace Haraltd.DataTypes.Events;

public interface IDeviceEvent
{
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Name { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("associated_adapter")]
    public string AssociatedAdapter { get; set; }

    [JsonPropertyName("connected")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? OptionConnected { get; set; }

    [JsonPropertyName("paired")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? OptionPaired { get; set; }

    [JsonPropertyName("blocked")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? OptionBlocked { get; set; }

    [JsonPropertyName("bonded")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? OptionBonded { get; set; }

    [JsonPropertyName("rssi")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public short? OptionRssi { get; set; }

    [JsonPropertyName("percentage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? OptionPercentage { get; set; }

    [JsonPropertyName("uuids")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Guid[] OptionUuiDs { get; set; }

    public void AppendEventProperties(ref StringBuilder stringBuilder, EventAction action);

    public void ResetProperties();
}

public abstract record DeviceEventBaseModel : IDeviceEvent
{
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string AssociatedAdapter { get; set; } = "";

    public bool? OptionConnected { get; set; }
    public bool? OptionPaired { get; set; }
    public bool? OptionBlocked { get; set; }
    public bool? OptionBonded { get; set; }
    public short? OptionRssi { get; set; }
    public int? OptionPercentage { get; set; }
    public Guid[] OptionUuiDs { get; set; }

    public void AppendEventProperties(
        ref StringBuilder stringBuilder,
        EventAction action = EventAction.None
    )
    {
        stringBuilder.AppendLine(
            $"({(action == EventAction.None ? "" : action + " ")})Name: {Name}"
        );
        stringBuilder.AppendLine($"Address: {Address}");
        stringBuilder.AppendLine($"Adapter: {AssociatedAdapter}");

        OptionConnected.AppendString("Connected", ref stringBuilder);
        OptionPaired.AppendString("Paired", ref stringBuilder);
        OptionBlocked.AppendString("Blocked", ref stringBuilder);
        OptionBonded.AppendString("Bonded", ref stringBuilder);
        OptionRssi.AppendString("RSSI", ref stringBuilder);
        OptionPercentage.AppendString("Percentage", ref stringBuilder);

        if (OptionUuiDs is { Length: > 0 })
        {
            stringBuilder.AppendLine("Profiles:");
            foreach (var uuid in OptionUuiDs)
            {
                var serviceName = BluetoothService.GetName(uuid);
                if (string.IsNullOrEmpty(serviceName))
                    serviceName = "Unknown";
                stringBuilder.AppendLine($"{serviceName} = {uuid}");
            }
        }

        stringBuilder.AppendLine();
    }

    public void ResetProperties()
    {
        Address = null;
        AssociatedAdapter = null;
        OptionConnected = null;
        OptionPaired = null;
        OptionBlocked = null;
        OptionRssi = null;
        OptionPercentage = null;
        OptionUuiDs = null;
    }
}

public record DeviceEvent : DeviceModel, IEvent
{
    EventType IEvent.Event => EventTypes.EventDevice;

    public EventAction Action { get; set; } = EventAction.Added;

    public new string ToConsoleString()
    {
        StringBuilder stringBuilder = new();
        AppendEventProperties(ref stringBuilder, Action);

        return stringBuilder.ToString();
    }

    public new void WriteJsonToStream(Utf8JsonWriter writer)
    {
        writer.WritePropertyName(SerializableContext.DeviceEventPropertyName);
        if (Action == EventAction.Added)
        {
            (this as IDevice).SerializeAll(writer);
            return;
        }

        (this as IDeviceEvent).SerializeSelected(writer);
    }
}
