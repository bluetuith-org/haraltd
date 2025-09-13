using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.Serializer;
using static Haraltd.DataTypes.Events.IFileTransferEvent;
using static Haraltd.DataTypes.Generic.IEvent;

namespace Haraltd.DataTypes.Events;

public interface IFileTransfer : IFileTransferEvent
{
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Name { get; set; }

    [JsonPropertyName("filename")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string FileName { get; set; }

    [JsonPropertyName("receiving")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Receiving { get; set; }
}

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

    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("size")]
    public long FileSize { get; set; }

    [JsonPropertyName("transferred")]
    public long BytesTransferred { get; set; }

    [JsonPropertyName("status")]
    public TransferStatus Status { get; set; }

    [JsonPropertyName("transfer_id")]
    public string TransferId { get; set; }

    [JsonPropertyName("session_id")]
    public string SessionId { get; set; }

    public static string GenerateSessionId()
    {
        return $"sid{Random.Shared.Next(1, ushort.MaxValue)}";
    }

    public static string GenerateTransferId(string sessionId)
    {
        return $"{sessionId}/tid{Random.Shared.Next(1, ushort.MaxValue)}";
    }
}

public abstract record FileTransferEventBaseModel : IFileTransferEvent
{
    public string Address { get; set; } = "";

    public long FileSize { get; set; }

    public long BytesTransferred { get; set; }

    public TransferStatus Status { get; set; }

    public string TransferId { get; set; }

    public string SessionId { get; set; }

    protected FileTransferEventBaseModel(bool mustGenerateIds)
    {
        if (mustGenerateIds)
            RegenerateIds();
    }

    public void RegenerateIds()
    {
        SessionId = GenerateSessionId();
        TransferId = GenerateTransferId(SessionId);
    }

    public void AppendEventProperties(ref StringBuilder stringBuilder)
    {
        stringBuilder.Append($"Address: {Address}, ");
        stringBuilder.Append($"Progress: {BytesTransferred} b/{FileSize} b");
    }
}

public record FileTransferEvent : FileTransferEventBaseModel, IEvent
{
    EventType IEvent.Event => EventTypes.EventFileTransfer;

    public EventAction Action { get; set; } = EventAction.Added;

    protected FileTransferEvent(bool mustGenerateIds)
        : base(mustGenerateIds) { }

    public virtual string ToConsoleString()
    {
        StringBuilder stringBuilder = new();

        AppendEventProperties(ref stringBuilder);
        stringBuilder.AppendLine();

        return stringBuilder.ToString();
    }

    public virtual void WriteJsonToStream(Utf8JsonWriter writer)
    {
        writer.WritePropertyName(SerializableContext.FileTransferEventPropertyName);
        (this as IFileTransferEvent).SerializeSelected(writer);
    }
}

public record FileTransferEventCombined : FileTransferEvent, IFileTransfer
{
    public string Name { get; set; }

    public string FileName { get; set; }
    public bool Receiving { get; set; }

    public FileTransferEventCombined(bool Receiving)
        : base(true)
    {
        this.Receiving = Receiving;
    }

    public FileTransferEventCombined(bool Receiving, FileTransferModel dataToMerge)
        : base(false)
    {
        this.Receiving = Receiving;

        Name = dataToMerge.Name;
        FileName = dataToMerge.FileName;
        Address = dataToMerge.Address;
        FileSize = dataToMerge.FileSize;
        Status = dataToMerge.Status;

        SessionId = dataToMerge.SessionId;
        TransferId = dataToMerge.TransferId;
    }

    public override void WriteJsonToStream(Utf8JsonWriter writer)
    {
        writer.WritePropertyName(SerializableContext.FileTransferEventPropertyName);
        if (Action == EventAction.Added)
            (this as IFileTransfer).SerializeAll(writer);
        else
            (this as IFileTransferEvent).SerializeSelected(writer);
    }
}
