using Bluetuith.Shim.Types;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bluetuith.Shim.Stack.Events;

public record class FileTransferEvent : Event
{
    public string Address = "";
    public string FileName = "";
    public long FileSize = 0;
    public long BytesTransferred = 0;

    public FileTransferEvent(EventCode eventCode)
    {
        EventType = eventCode;
    }

    public sealed override string ToConsoleString()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.Append($"[+] Address: {Address}, Filename: {FileName}, ");
        stringBuilder.Append($"Progress: {BytesTransferred} b/{FileSize} b");
        stringBuilder.AppendLine();

        return stringBuilder.ToString();
    }

    public sealed override JsonObject ToJsonObject()
    {
        return new JsonObject()
        {
            ["fileTransferEvent"] = JsonSerializer.SerializeToNode(
                new JsonObject()
                {
                    ["type"] = EventType.Name,
                    ["address"] = Address,
                    ["fileName"] = FileName,
                    ["fileSize"] = FileSize,
                    ["bytesTransferred"] = BytesTransferred,
                }
            )
        };
    }
}
