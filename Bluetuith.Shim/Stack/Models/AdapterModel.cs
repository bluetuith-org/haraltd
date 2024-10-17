using Bluetuith.Shim.Types;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bluetuith.Shim.Stack.Models;

public record class AdapterModel : Result
{
    public string Address { get; protected set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; protected set; } = "";
    public string Manufacturer { get; protected set; } = "";

    public bool Powered { get; protected set; } = false;
    public bool Discoverable { get; protected set; } = false;
    public bool Pairable { get; protected set; } = false;

    public bool IsAvailable { get; protected set; } = false;
    public bool IsEnabled { get; protected set; } = false;
    public bool IsOperable => IsAvailable && IsEnabled;

    public AdapterModel() { }

    public sealed override string ToConsoleString()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine($"Name: {Name}");
        stringBuilder.AppendLine($"Address: {Address}");
        stringBuilder.AppendLine($"Manufacturer: {Manufacturer}");
        stringBuilder.AppendLine($"Version: {Version}");

        stringBuilder.AppendLine($"Powered: {(Powered ? "yes" : "no")}");
        stringBuilder.AppendLine($"Discoverable: {(Discoverable ? "yes" : "no")}");
        stringBuilder.AppendLine($"Pairable: {(Pairable ? "yes" : "no")}");

        stringBuilder.AppendLine($"Available: {IsAvailable}");
        stringBuilder.AppendLine($"Enabled: {IsEnabled}");
        stringBuilder.AppendLine($"Operable: {IsOperable}");

        return stringBuilder.ToString();
    }

    public sealed override JsonObject ToJsonObject()
    {
        return new JsonObject
        {
            ["adapterProperties"] = JsonSerializer.SerializeToNode(
                new JsonObject()
                {
                    ["name"] = Name,
                    ["address"] = Address,
                    ["version"] = Version,
                    ["manufacturer"] = Manufacturer,
                    ["powered"] = Powered,
                    ["discoverable"] = Discoverable,
                    ["pairable"] = Pairable,
                    ["isAvailable"] = IsAvailable,
                    ["isEnabled"] = IsEnabled
                }
            )
        };
    }
}
