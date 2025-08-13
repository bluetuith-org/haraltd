using System.Text.Json;
using System.Text.Json.Serialization;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Serializer;
using static Haraltd.DataTypes.Generic.IEvent;

namespace Haraltd.DataTypes.Events;

public record MessageReceivedEvent : MessageEvent, IEvent
{
    EventType IEvent.Event => EventTypes.EventDevice;

    public EventAction Action { get; set; } = EventAction.Added;

    public string ToConsoleString()
    {
        return $"[+] New message event from device {Address} (folder {Folder}){Environment.NewLine}";
    }

    public void WriteJsonToStream(Utf8JsonWriter writer)
    {
        writer.WritePropertyName(SerializableContext.MessageHandleEventPropertyName);
        (this as MessageEvent).SerializeSelected(writer);
    }
}

public record MessageEvent
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("folder")]
    public string Folder { get; set; }

    [JsonPropertyName("handle")]
    public string Handle { get; set; }

    [JsonPropertyName("message_event_type")]
    public string MessageEventType { get; set; }

    [JsonPropertyName("message_type")]
    public string MessageType { get; set; }
}
