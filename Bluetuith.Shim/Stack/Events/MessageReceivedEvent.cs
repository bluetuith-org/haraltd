using Bluetuith.Shim.Types;
using GoodTimeStudio.MyPhone.OBEX;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bluetuith.Shim.Stack.Events;

public record class MessageReceivedEvent : Event
{
    private string Address { get; set; }
    public MessageReceivedEventArgs MessageEventArgs { get; set; }

    public MessageReceivedEvent(string address, MessageReceivedEventArgs messageEventArgs)
    {
        Address = address;
        MessageEventArgs = messageEventArgs;
        EventType = StackEventCode.MessageAccessServerEventCode;
    }

    public override string ToConsoleString()
    {
        return $"[+] New message event from device {Address} (handle {MessageEventArgs.MessageHandle}, folder {MessageEventArgs.Folder}){Environment.NewLine}";
    }

    public override JsonObject ToJsonObject()
    {
        return new JsonObject()
        {
            ["messageHandleEvent"] = JsonSerializer.SerializeToNode(
                new JsonObject()
                {
                    ["address"] = Address,
                    ["folder"] = MessageEventArgs.Folder,
                    ["handle"] = MessageEventArgs.MessageHandle,
                    ["eventType"] = MessageEventArgs.EventType,
                    ["messageType"] = MessageEventArgs.MessageType,
                }
            )
        };
    }
}
