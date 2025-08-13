using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Serializer;
using static Haraltd.DataTypes.Events.IFileTransferEvent;
using static Haraltd.DataTypes.Generic.IEvent;

namespace Haraltd.DataTypes.Events;

public interface IFileTransferEvent
{
    [JsonConverter(typeof(JsonStringEnumConverter<TransferStatus>))]
    public enum TransferStatus
    {
        Queued,
        Active,
        Suspended,
        Complete,
        Error,
    }

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Name { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("filename")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string FileName { get; set; }

    [JsonPropertyName("size")]
    public long FileSize { get; set; }

    [JsonPropertyName("transferred")]
    public long BytesTransferred { get; set; }

    [JsonPropertyName("status")]
    public TransferStatus Status { get; set; }

    public void AppendEventProperties(ref StringBuilder stringBuilder);
}

public abstract record FileTransferEventBaseModel : IFileTransferEvent
{
    public string Address { get; set; } = "";

    public string Name { get; set; } = "";

    public string FileName { get; set; } = "";

    public long FileSize { get; set; }

    public long BytesTransferred { get; set; }

    public TransferStatus Status { get; set; }

    public void AppendEventProperties(ref StringBuilder stringBuilder)
    {
        stringBuilder.Append($"Name: {Name}, ");
        stringBuilder.Append($"File: {FileName}, ");
        stringBuilder.Append($"Address: {Address}, ");
        stringBuilder.Append($"Progress: {BytesTransferred} b/{FileSize} b");
    }
}

public record FileTransferEvent : FileTransferEventBaseModel, IEvent
{
    EventType IEvent.Event => EventTypes.EventFileTransfer;

    public EventAction Action { get; set; } = EventAction.Added;

    public string ToConsoleString()
    {
        StringBuilder stringBuilder = new();

        AppendEventProperties(ref stringBuilder);
        stringBuilder.AppendLine();

        return stringBuilder.ToString();
    }

    public void WriteJsonToStream(Utf8JsonWriter writer)
    {
        writer.WritePropertyName(SerializableContext.FileTransferEventPropertyName);
        (this as IFileTransferEvent).SerializeAll(writer);
    }
}
