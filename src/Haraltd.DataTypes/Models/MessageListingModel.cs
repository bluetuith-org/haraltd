using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Serializer;

namespace Haraltd.DataTypes.Models;

public record MessageListingModel : IResult
{
    private readonly List<MessageItem> _messageList;

    public MessageListingModel(List<MessageItem> listing)
    {
        _messageList = listing;
    }

    public string ToConsoleString()
    {
        StringBuilder stringBuilder = new();
        foreach (var message in _messageList)
        {
            stringBuilder.AppendLine();

            stringBuilder.AppendLine(
                $"Message-Handle:{message.Handle} Size: {message.Size}, Date:{message.DateTime}"
            );
            stringBuilder.AppendLine(
                $"Recipient: {message.RecipientAddressing}, Status: {message.ReceptionStatus}"
            );
            stringBuilder.AppendLine($"Subject: {message.Subject}");

            _ =
                message.AttachmentSize > 0
                    ? stringBuilder.AppendLine(
                        $"Attachments: yes, {message.AttachmentSize / 1024} kb"
                    )
                    : stringBuilder.AppendLine("Attachments: no");
        }

        return stringBuilder.ToString();
    }

    public void WriteJsonToStream(Utf8JsonWriter writer)
    {
        writer.WriteStartArray(SerializableContext.MessageListPropertyName);
        foreach (var message in _messageList)
            message.SerializeSelected(writer);
        writer.WriteEndArray();
    }
}

public class MessageItem
{
    [JsonPropertyName("handle")]
    public string Handle { get; set; }

    [JsonPropertyName("attachment_size")]
    public int AttachmentSize { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("date_time")]
    public string DateTime { get; set; }

    [JsonPropertyName("recipient_addressing")]
    public string RecipientAddressing { get; set; }

    [JsonPropertyName("reception_status")]
    public string ReceptionStatus { get; set; }

    [JsonPropertyName("subject")]
    public string Subject { get; set; }
}
