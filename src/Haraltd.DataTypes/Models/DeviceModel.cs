using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Serializer;

namespace Haraltd.DataTypes.Models;

public interface IDevice : IDeviceEvent
{
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Name { get; set; }

    [JsonPropertyName("alias")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Alias { get; set; }

    [JsonPropertyName("class")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public uint Class { get; set; }

    [JsonPropertyName("legacy_pairing")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool LegacyPairing { get; set; }
}

public abstract record DeviceBaseModel : DeviceEventBaseModel, IDevice
{
    public string Name { get; set; } = "";
    public string Alias { get; set; } = "";
    public uint Class { get; set; } = 0;
    public bool LegacyPairing { get; set; } = false;
}

public record DeviceModel : DeviceBaseModel, IResult
{
    public string ToConsoleString()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine($"Name: {Name}");

        AppendEventProperties(ref stringBuilder);

        return stringBuilder.ToString();
    }

    public void WriteJsonToStream(Utf8JsonWriter writer)
    {
        writer.WritePropertyName(SerializableContext.DevicePropertyName);
        (this as IDevice).SerializeAll(writer);
    }
}

public static class DeviceModelExtensions
{
    public static GenericResult<List<DeviceModel>> ToResult(
        this List<DeviceModel> devices,
        string consoleObject,
        string jsonObject
    )
    {
        return new GenericResult<List<DeviceModel>>(
            () =>
            {
                StringBuilder stringBuilder = new();

                stringBuilder.AppendLine(consoleObject);
                foreach (var device in devices)
                    stringBuilder.AppendLine(device.ToConsoleString());

                return stringBuilder.ToString();
            },
            writer =>
            {
                writer.WriteStartArray(jsonObject);
                foreach (var device in devices)
                    (device as IDevice).SerializeAll(writer);
                writer.WriteEndArray();
            }
        );
    }
}
