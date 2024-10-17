using Bluetuith.Shim.Types;
using MixERP.Net.VCards;
using MixERP.Net.VCards.Models;
using System.Text;
using System.Text.Json.Nodes;

namespace Bluetuith.Shim.Stack.Models;

public record class VcardModel : Result
{
    private readonly string VCardType = "";
    private readonly string VCards = "";

    public VcardModel() { }

    public VcardModel(string vcardType, string vcards)
    {
        VCardType = vcardType;
        VCards = vcards;
    }

    public sealed override string ToConsoleString()
    {
        StringBuilder stringBuilder = new();

        stringBuilder.AppendLine($" = {VCardType} = ");
        foreach (VCard? card in Deserializer.GetVCards(VCards))
        {
            if (card == null)
                continue;

            stringBuilder.AppendLine($"# {card.FormattedName}:");
            stringBuilder.AppendLine("Telephones:");
            if (card.Telephones != null)
            {
                foreach (Telephone? telephone in card.Telephones)
                {
                    if (telephone == null)
                        continue;

                    stringBuilder.AppendLine($"{telephone.Number}");
                }
            }

            stringBuilder.AppendLine("");
        }

        return stringBuilder.ToString();
    }

    public sealed override JsonObject ToJsonObject()
    {
        return new JsonObject()
        {
            ["vCardType"] = VCardType,
            ["vcards"] = VCards,
        };
    }
}
