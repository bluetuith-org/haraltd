using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.Serializer;
using static Haraltd.DataTypes.Generic.IEvent;

namespace Haraltd.DataTypes.Events;

public interface IAdapterEvent
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("powered")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? OptionPowered { get; set; }

    [JsonPropertyName("discoverable")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? OptionDiscoverable { get; set; }

    [JsonPropertyName("pairable")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? OptionPairable { get; set; }

    [JsonPropertyName("discovering")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? OptionDiscovering { get; set; }

    public void PrintEventProperties(ref StringBuilder stringBuilder);

    public void ResetProperties();
}

public abstract record AdapterEventBaseModel : IAdapterEvent
{
    public string Address { get; set; } = "";
    public bool? OptionPowered { get; set; }
    public bool? OptionDiscoverable { get; set; }
    public bool? OptionPairable { get; set; }
    public bool? OptionDiscovering { get; set; }

    public void PrintEventProperties(ref StringBuilder stringBuilder)
    {
        stringBuilder.AppendLine($"Address: {Address}");

        OptionPowered.AppendString("Powered", ref stringBuilder);
        OptionDiscoverable.AppendString("Discoverable", ref stringBuilder);
        OptionPairable.AppendString("Pairable", ref stringBuilder);
        OptionDiscovering.AppendString("Discovering", ref stringBuilder);
    }

    public void ResetProperties()
    {
        Address = null;
        OptionPowered = null;
        OptionDiscoverable = null;
        OptionPairable = null;
        OptionDiscovering = null;
    }
}

public record AdapterEvent : AdapterModel, IEvent
{
    EventType IEvent.Event => EventTypes.EventAdapter;

    public EventAction Action { get; set; } = EventAction.Added;

    public new string ToConsoleString()
    {
        StringBuilder stringBuilder = new();
        PrintEventProperties(ref stringBuilder);

        return stringBuilder.ToString();
    }

    public new void WriteJsonToStream(Utf8JsonWriter writer)
    {
        writer.WritePropertyName(SerializableContext.AdapterEventPropertyName);
        if (Action == EventAction.Added)
        {
            (this as IAdapter).SerializeAll(writer);
            return;
        }

        (this as IAdapterEvent).SerializeSelected(writer);
    }
}
