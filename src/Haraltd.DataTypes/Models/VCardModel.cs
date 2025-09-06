using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Serializer;
using MixERP.Net.VCards;

namespace Haraltd.DataTypes.Models;

public interface IVcard
{
    [JsonPropertyName("vcard_type")]
    string VCardType { get; }

    [JsonPropertyName("vcard_data")]
    string VCards { get; }
}

public record VcardModel : IResult, IVcard
{
    public VcardModel() { }

    public VcardModel(string vcardType, string vcards)
    {
        VCardType = vcardType;
        VCards = vcards;
    }

    public string ToConsoleString()
    {
        StringBuilder stringBuilder = new();

        stringBuilder.AppendLine($" = {VCardType} = ");

#nullable enable
        foreach (var card in Deserializer.GetVCards(VCards))
        {
            if (card == null)
                continue;

            stringBuilder.AppendLine($"# {card.FormattedName}:");
            stringBuilder.AppendLine("Telephones:");
            if (card.Telephones != null)
                foreach (var telephone in card.Telephones)
                {
                    if (telephone == null)
                        continue;

                    stringBuilder.AppendLine($"{telephone.Number}");
                }

            stringBuilder.AppendLine("");
        }
#nullable disable

        return stringBuilder.ToString();
    }

    public void WriteJsonToStream(Utf8JsonWriter writer)
    {
        writer.WritePropertyName(SerializableContext.VcardPropertyName);
        (this as IVcard).SerializeSelected(writer);
    }

    public string VCardType { get; } = "";
    public string VCards { get; } = "";
}
