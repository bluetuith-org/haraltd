using System.Text.Json;
using System.Text.Json.Serialization;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Serializer;

namespace Haraltd.DataTypes.Models;

public struct PlatformInfo : IResult
{
    [JsonPropertyName("stack")]
    public string Stack { get; set; }

    [JsonPropertyName("os_info")]
    public string OsInfo { get; set; }

    public readonly string ToConsoleString()
    {
        return $"Stack: {Stack} ({OsInfo})";
    }

    public readonly void WriteJsonToStream(Utf8JsonWriter writer)
    {
        writer.WritePropertyName(SerializableContext.PlatformPropertyNme);
        this.SerializeAll(writer);
    }
}
