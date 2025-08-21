﻿using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Serializer;

namespace Haraltd.DataTypes.Models;

public record BMessagesModel : IResult
{
    private readonly List<BMessageItem> _bMessageList;

    public BMessagesModel(List<BMessageItem> listing)
    {
        _bMessageList = listing;
    }

    public BMessagesModel(BMessageItem message)
    {
        _bMessageList = [message];
    }

    public string ToConsoleString()
    {
        StringBuilder stringBuilder = new();
        foreach (var message in _bMessageList)
        {
            stringBuilder.AppendLine();

            if (message.Sender != null)
                stringBuilder.AppendLine($"Sender: {message.Sender}");

            stringBuilder.AppendLine($"Folder: {message.Folder}, Status: {message.Status}");
            stringBuilder.AppendLine($"{message.Body}");
        }

        return stringBuilder.ToString();
    }

    public void WriteJsonToStream(Utf8JsonWriter writer)
    {
        writer.WriteStartArray(SerializableContext.BMessagePropertyName);
        foreach (var message in _bMessageList)
            message.SerializeSelected(writer);
        writer.WriteEndArray();
    }
}

public class BMessageItem
{
    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("folder")]
    public string Folder { get; set; }

    [JsonPropertyName("charset")]
    public string Charset { get; set; }

    [JsonPropertyName("length")]
    public int Length { get; set; }

    [JsonPropertyName("sender")]
    public string Sender { get; set; }

    [JsonPropertyName("body")]
    public string Body { get; set; }
}
