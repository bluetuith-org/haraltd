using Bluetuith.Shim.Types;
using InTheHand.Net.Bluetooth;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bluetuith.Shim.Stack.Models;

public record class DeviceModel : Result
{
    public class DeviceClass
    {
        public int MajorDevice { get; set; }
        public int Device { get; set; }
        public int Service { get; set; }
    }

    public string Address { get; protected set; } = "";
    public string Name { get; protected set; } = "";

    public DeviceClass Class { get; protected set; } = new DeviceClass();
    public Guid[] UUIDs { get; protected set; } = [];

    public bool Connected { get; protected set; } = false;
    public bool Paired { get; protected set; } = false;

    public DeviceModel() { }

    public sealed override string ToConsoleString()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine($"Name: {Name}");
        stringBuilder.AppendLine($"Address: {Address}");

        stringBuilder.AppendLine($"Connected: {(Connected ? "yes" : "no")}");
        stringBuilder.AppendLine($"Paired: {(Paired ? "yes" : "no")}");

        if (UUIDs.Length > 0)
        {
            stringBuilder.AppendLine("Profiles:");
            foreach (Guid uuid in UUIDs)
            {
                var serviceName = BluetoothService.GetName(uuid);
                if (string.IsNullOrEmpty(serviceName))
                {
                    serviceName = "Unknown";
                }

                stringBuilder.AppendLine($"{serviceName} = {uuid}");
            }
        }

        return stringBuilder.ToString();
    }

    public sealed override JsonObject ToJsonObject()
    {
        return new JsonObject
        {
            ["deviceProperties"] = JsonSerializer.SerializeToNode(
                new JsonObject()
                {
                    ["name"] = Name,
                    ["address"] = Address,
                    ["deviceClass.majorDevice"] = Class.MajorDevice,
                    ["deviceClass.device"] = Class.Device,
                    ["deviceClass.service"] = Class.Service,
                    ["serviceUuids"] = JsonSerializer.SerializeToNode(UUIDs),
                    ["connected"] = Connected,
                    ["paired"] = Paired,
                }
            )
        };
    }
}

public static class DeviceModelExtensions
{
    public static GenericResult<List<DeviceModel>> ToResult(this List<DeviceModel> devices, string consoleObject, string jsonObject)
    {
        return new GenericResult<List<DeviceModel>>(
            consoleFunc: () =>
            {
                StringBuilder stringBuilder = new();

                stringBuilder.AppendLine(consoleObject);
                foreach (DeviceModel device in devices)
                {
                    stringBuilder.AppendLine(device.ToConsoleString());
                }

                return stringBuilder.ToString();
            },
            jsonObjectFunc: () =>
            {
                JsonArray array = [];
                foreach (DeviceModel device in devices)
                {
                    array.Add(device.ToJsonObject());
                }

                return new JsonObject()
                {
                    [consoleObject] = JsonSerializer.SerializeToNode(array)
                };
            }
        );
    }
}
