using System.Text.Json;
using Haraltd.DataTypes.Generic;

namespace Haraltd.DataTypes.Models;

public readonly struct Features(Features.FeatureFlags flags) : IResult
{
    [Flags]
    public enum FeatureFlags : uint
    {
        FeatureConnection = 1 << 1,
        FeaturePairing = 1 << 2,
        FeatureSendFile = 1 << 3,
        FeatureReceiveFile = 1 << 4,
    }

    public string ToConsoleString()
    {
        return $"Features: {flags}";
    }

    public void WriteJsonToStream(Utf8JsonWriter writer)
    {
        writer.WriteNumber(Serializer.SerializableContext.FeaturesPropertyName, (int)flags);
    }
}
