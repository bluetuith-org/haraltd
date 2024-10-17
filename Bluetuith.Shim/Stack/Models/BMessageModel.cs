using Bluetuith.Shim.Types;
using GoodTimeStudio.MyPhone.OBEX;
using MixERP.Net.VCards.Serializer;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bluetuith.Shim.Stack.Models;

public record class BMessageModel : Result
{
    private readonly List<BMessage> bMessageList;

    public BMessageModel(List<BMessage> listing)
    {
        bMessageList = listing;
    }

    public BMessageModel(BMessage message)
    {
        bMessageList = [message];
    }

    public override string ToConsoleString()
    {
        StringBuilder stringBuilder = new();
        foreach (BMessage message in bMessageList)
        {
            stringBuilder.AppendLine();

            if (message.Sender != null)
            {
                stringBuilder.AppendLine($"Sender: {message.Sender.FirstName} {message.Sender.LastName}");
            }

            stringBuilder.AppendLine($"Folder: {message.Folder}, Status: {message.Status}");
            stringBuilder.AppendLine($"{message.Body}");
        }

        return stringBuilder.ToString();
    }

    public override JsonObject ToJsonObject()
    {
        JsonArray jsonArray = [];
        foreach (BMessage message in bMessageList)
        {
            jsonArray.Add(new JsonObject()
            {
                ["status"] = message.Status.ToString(),
                ["type"] = message.Type,
                ["folder"] = message.Folder,
                ["charset"] = message.Charset,
                ["length"] = message.Length,
                ["sender"] = message.Sender.Serialize(),
                ["body"] = message.Body,
            });
        }

        return new JsonObject()
        {
            ["bMessageList"] = JsonSerializer.SerializeToNode(jsonArray)
        };
    }
}
