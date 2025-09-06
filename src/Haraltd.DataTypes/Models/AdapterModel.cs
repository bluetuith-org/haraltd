using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Serializer;
using InTheHand.Net.Bluetooth;

namespace Haraltd.DataTypes.Models;

public interface IAdapter : IAdapterEvent
{
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Name { get; set; }

    [JsonPropertyName("alias")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Alias { get; set; }

    [JsonPropertyName("unique_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string UniqueName { get; set; }

    [JsonPropertyName("uuids")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Guid[] UuiDs { get; set; }
}

public abstract record AdapterBaseModel : AdapterEventBaseModel, IAdapter
{
    public string Name { get; set; } = "";
    public string Alias { get; set; } = "";
    public string UniqueName { get; set; } = "";
    public Guid[] UuiDs { get; set; }

    protected void PrintProperties(ref StringBuilder stringBuilder)
    {
        stringBuilder.AppendLine($"Name: {Name}");
        if (UuiDs is { Length: > 0 })
        {
            stringBuilder.AppendLine("Profiles:");
            foreach (var uuid in UuiDs)
            {
                var serviceName = BluetoothService.GetName(uuid);
                if (string.IsNullOrEmpty(serviceName))
                    serviceName = "Unknown";
                stringBuilder.AppendLine($"{serviceName} = {uuid}");
            }
        }
    }
}

public record AdapterModel : AdapterBaseModel, IResult
{
    public string ToConsoleString()
    {
        StringBuilder stringBuilder = new();

        PrintEventProperties(ref stringBuilder);
        PrintProperties(ref stringBuilder);

        return stringBuilder.ToString();
    }

    public void WriteJsonToStream(Utf8JsonWriter writer)
    {
        writer.WritePropertyName(SerializableContext.AdapterPropertyName);
        (this as IAdapter).SerializeAll(writer);
    }
}

public static class AdapterModelExtensions
{
    public static GenericResult<List<AdapterModel>> ToResult(
        this List<AdapterModel> adapters,
        string consoleObject,
        string jsonObject
    )
    {
        return new GenericResult<List<AdapterModel>>(
            () =>
            {
                StringBuilder stringBuilder = new();

                stringBuilder.AppendLine(consoleObject);
                foreach (var adapter in adapters)
                    stringBuilder.AppendLine(adapter.ToConsoleString());

                return stringBuilder.ToString();
            },
            writer =>
            {
                writer.WriteStartArray(jsonObject);
                foreach (var adapter in adapters)
                    (adapter as IAdapter).SerializeAll(writer);
                writer.WriteEndArray();
            }
        );
    }
}
