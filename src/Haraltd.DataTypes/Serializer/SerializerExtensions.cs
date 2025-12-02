using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Haraltd.DataTypes.Serializer;

public static class SerializerExtensions
{
    private static readonly JsonSerializerOptions Options = new(SerializableContext.Default.Options)
    {
        TypeInfoResolver = SerializableContext.Default.WithAddedModifier(IgnoreEmptyStringModifier),
    };

    private static void IgnoreEmptyStringModifier(JsonTypeInfo typeInfo)
    {
        if (typeInfo is not { Kind: JsonTypeInfoKind.Object })
            return;

        foreach (var property in typeInfo.Properties)
        {
            if (property == null || property.PropertyType != typeof(string))
                continue;

            var ignoreAttr =
                property.AttributeProvider?.GetCustomAttributes(typeof(JsonIgnoreAttribute), false)
                    is not { } attr
                || attr.Length == 0
                    ? null
                    : (JsonIgnoreAttribute)attr[0];

            if (
                ignoreAttr is not null
                && ignoreAttr.Condition == JsonIgnoreCondition.WhenWritingDefault
            )
                property.ShouldSerialize = (_, value) =>
                    value is string val && !string.IsNullOrEmpty(val);
        }
    }

    public static void SerializeAll<T>(this T result, Utf8JsonWriter writer)
    {
        var typeInfo = Options.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>;
        JsonSerializer.Serialize(writer, result, typeInfo);
    }

    public static void SerializeSelected<T>(this T result, Utf8JsonWriter writer)
    {
        var typeInfo = Options.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>;
        JsonSerializer.Serialize(writer, result, typeInfo);
    }
}
