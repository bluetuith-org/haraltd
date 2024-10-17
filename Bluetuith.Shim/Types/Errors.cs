using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bluetuith.Shim.Types;

public record class ErrorCode
{
    public static ErrorCode ERROR_NONE = new("ERROR_NONE", 0);
    public static ErrorCode ERROR_UNEXPECTED = new("ERROR_UNEXPECTED", -1);
    public static ErrorCode ERROR_OPERATION_CANCELLED = new("ERROR_OPERATION_CANCELLED", -2);
    public static ErrorCode ERROR_OPERATION_IN_PROGRESS = new("ERROR_OPERATION_IN_PROGRESS", -3);

    public string Name { get; }
    public int Value { get; }

    public ErrorCode(string name, int code)
    {
        Name = name;
        Value = code;
    }

    public sealed override string ToString()
    {
        return @$"{Name} ({Value})";
    }
}

public record class ErrorData : Result
{
    public ErrorCode Code { get; }
    public string Description { get; }
    public Dictionary<string, object> Metadata { get; }

    public ErrorData(ErrorCode Code, string Description, Dictionary<string, object> Metadata)
    {
        this.Code = Code;
        this.Description = Description;
        this.Metadata = Metadata;
    }

    public sealed override string ToConsoleString()
    {
        if (Code == Errors.ErrorNone.Code)
        {
            return "";
        }

        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine($"ERROR: {Description} ({Code})");
        foreach ((var property, var value) in Metadata)
        {
            stringBuilder.AppendLine($"{property}: {value}");
        }

        return stringBuilder.ToString();
    }

    public sealed override JsonObject ToJsonObject()
    {
        return Code == Errors.ErrorNone.Code
            ? ([])
            : new JsonObject()
            {
                ["code"] = Code.Value,
                ["name"] = Code.Name,
                ["description"] = Description,
                ["metadata"] = JsonSerializer.SerializeToNode(Metadata),
            };
    }
}

public class Errors
{
    public static readonly ErrorData ErrorNone = new(
        Code: ErrorCode.ERROR_NONE,
        Description: "",
        Metadata: []
    );

    public static readonly ErrorData ErrorUnexpected = new(
        Code: ErrorCode.ERROR_UNEXPECTED,
        Description: "An unexpected error occurred",
        Metadata: new() {
            {"exception", ""},
        }
    );

    public static readonly ErrorData ErrorOperationCancelled = new(
        Code: ErrorCode.ERROR_OPERATION_CANCELLED,
        Description: "An operation was cancelled",
        Metadata: new() {
            {"operation", ""},
        }
    );

    public static readonly ErrorData ErrorOperationInProgress = new(
        Code: ErrorCode.ERROR_OPERATION_IN_PROGRESS,
        Description: "The specified operation is in progress",
        Metadata: new() {
            {"operation", ""},
        }
    );
}

public static class ErrorExtensions
{
    public static void AddMetadata(this Dictionary<string, object> dict, string key, object value)
    {

    }

    public static ErrorData WrapError(this ErrorData e, Dictionary<string, object> dict)
    {
        foreach ((var property, var value) in dict)
        {
            if (e.Metadata.ContainsKey(property))
            {
                e.Metadata[property] = value;
            }
            else
            {
                e.Metadata.Add(property, value);
            }
        }

        return e;
    }
}

[Serializable]
public class ErrorException
{
    public string Message { get; } = "";
    public System.Collections.IDictionary ExceptionMetadata { get; }

    public ErrorException(Exception inner)
    {
        Message = inner.Message;
        ExceptionMetadata = inner.Data;
    }
}

