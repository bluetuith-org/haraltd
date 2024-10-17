using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bluetuith.Shim.Types;

public abstract record class Result
{
    public virtual string ToConsoleString()
    {
        return "";
    }

    public virtual JsonObject ToJsonObject()
    {
        return [];
    }
}

public record class GenericResult<T> : Result
{
    public Func<string> ConsoleFunc { get; set; }
    public Func<JsonObject> JsonObjectFunc { get; set; }

    public GenericResult(Func<string> consoleFunc, Func<JsonObject> jsonObjectFunc)
    {
        ConsoleFunc = consoleFunc;
        JsonObjectFunc = jsonObjectFunc;
    }

    public override string ToConsoleString()
    {
        return ConsoleFunc != null ? ConsoleFunc() : base.ToConsoleString();
    }

    public override JsonObject ToJsonObject()
    {
        return JsonObjectFunc != null ? JsonObjectFunc() : base.ToJsonObject();
    }

    public static GenericResult<T> Empty()
    {
        return new GenericResult<T>(
            consoleFunc: () =>
            {
                return "";
            },
            jsonObjectFunc: () =>
            {
                return [];
            });
    }
}

public static class ResultExtensions
{
    public static GenericResult<List<string>> ToResult(this List<string> list, string consoleObjectName, string jsonObjectName)
    {
        return new GenericResult<List<string>>(
            consoleFunc: () =>
            {
                StringBuilder stringBuilder = new();
                stringBuilder.AppendLine($"= {consoleObjectName} =");
                foreach (var item in list)
                {
                    stringBuilder.AppendLine(item);
                }

                return stringBuilder.ToString();
            },
            jsonObjectFunc: () =>
            {
                return new JsonObject()
                {
                    [jsonObjectName] = JsonSerializer.SerializeToNode(list)
                };
            });
    }

    public static GenericResult<int> ToResult(this int value, string consoleObjectName, string jsonObjectName)
    {
        return new GenericResult<int>(
            consoleFunc: () =>
            {
                return $"{consoleObjectName}: {value}";
            },
            jsonObjectFunc: () =>
            {
                return new JsonObject()
                {
                    [jsonObjectName] = value
                };
            });
    }

    public static GenericResult<string> ToResult(this string value, string consoleObjectName, string jsonObjectName)
    {
        return new GenericResult<string>(
            consoleFunc: () =>
            {
                return $"{consoleObjectName}: {value}";
            },
            jsonObjectFunc: () =>
            {
                return new JsonObject()
                {
                    [jsonObjectName] = value
                };
            });
    }
}
