using System.Collections;
using System.Text;
using System.Text.Json;
using Haraltd.DataTypes.Serializer;

namespace Haraltd.DataTypes.Generic;

public interface IError : IErrorEvent
{
    public Dictionary<string, object> Metadata { get; }
}

public interface IErrorEvent : IEvent
{
    public ErrorCode Code { get; }
    public string Description { get; }
}

public record ErrorCode
{
    public static readonly ErrorCode ErrorNone = new("ERROR_NONE", 0);
    public static readonly ErrorCode ErrorUnexpected = new("ERROR_UNEXPECTED", -1);
    public static readonly ErrorCode ErrorOperationCancelled = new("ERROR_OPERATION_CANCELLED", -2);

    public static readonly ErrorCode ErrorOperationInProgress = new(
        "ERROR_OPERATION_IN_PROGRESS",
        -3
    );

    public static readonly ErrorCode ErrorUnsupported = new("ERROR_UNSUPPORTED", -100);

    public ErrorCode(string name, int code)
    {
        Name = name;
        Value = code;
    }

    public string Name { get; }
    public int Value { get; }

    public sealed override string ToString()
    {
        return $"{Name} ({Value})";
    }
}

public record ErrorEvent : IErrorEvent
{
    protected static readonly JsonEncodedText ErrorText = JsonEncodedText.Encode("error");
    protected static readonly JsonEncodedText CodeText = JsonEncodedText.Encode("code");

    protected static readonly JsonEncodedText DescriptionText = JsonEncodedText.Encode(
        "description"
    );

    public ErrorEvent(ErrorCode Code, string Description)
    {
        this.Code = Code;
        this.Description = Description;
    }

    public ErrorCode Code { get; }

    public string Description { get; }

    public EventType Event => EventTypes.EventError;

    IEvent.EventAction IEvent.Action
    {
        get => IEvent.EventAction.Added;
        set => throw new InvalidDataException();
    }

    public string ToConsoleString()
    {
        if (Code == Errors.ErrorNone.Code)
            return "";

        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine($"ERROR: {Description} ({Code})");

        return stringBuilder.ToString();
    }

    public void WriteJsonToStream(Utf8JsonWriter writer)
    {
        writer.WriteStartObject(ErrorText);

        writer.WriteNumber(CodeText, Code.Value);
        writer.WriteString(DescriptionText, Description);

        writer.WriteEndObject();
    }
}

public record ErrorData : ErrorEvent, IError
{
    private static readonly JsonEncodedText MetadataText = JsonEncodedText.Encode("metadata");

    public ErrorData(ErrorCode Code, string Description)
        : base(Code, Description) { }

    public Dictionary<string, object> Metadata { get; set; }

    public new string ToConsoleString()
    {
        if (Code == Errors.ErrorNone.Code)
            return "";

        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine($"ERROR: {Description} ({Code})");

        if (Metadata?.Count > 0)
            foreach (var (property, value) in Metadata)
                stringBuilder.AppendLine($"{property}: {value}");

        return stringBuilder.ToString();
    }

    public new void WriteJsonToStream(Utf8JsonWriter writer)
    {
        if (Code == Errors.ErrorNone.Code)
            return;

        writer.WriteStartObject(ErrorText);

        writer.WriteNumber(CodeText, Code.Value);
        writer.WriteString(DescriptionText, Description);

        if (Metadata?.Count > 0)
        {
            writer.WritePropertyName(MetadataText);
            Metadata.SerializeAll(writer);
        }

        writer.WriteEndObject();
    }
}

public partial class Errors
{
    public static readonly ErrorData ErrorNone = new(ErrorCode.ErrorNone, "");

    public static readonly ErrorData ErrorUnexpected = new(
        ErrorCode.ErrorUnexpected,
        "An unexpected error occurred"
    );

    public static readonly ErrorData ErrorOperationCancelled = new(
        ErrorCode.ErrorOperationCancelled,
        "An operation was cancelled"
    );

    public static readonly ErrorData ErrorOperationInProgress = new(
        ErrorCode.ErrorOperationInProgress,
        "The specified operation is in progress"
    );

    public static readonly ErrorData ErrorUnsupported = new(
        ErrorCode.ErrorUnsupported,
        "This operation is unsupported"
    );
}

public static class ErrorExtensions
{
    public static ErrorData AddMetadata(this ErrorData e, string key, object value)
    {
        e.Metadata ??= [];
        if (!e.Metadata.TryAdd(key, value))
            e.Metadata[key] = value;

        return e;
    }

    public static ErrorData WrapError(this ErrorData e, Dictionary<string, object> dict)
    {
        foreach (var (property, value) in dict)
            e.AddMetadata(property, value);

        return e;
    }
}

[Serializable]
public class ErrorException
{
    public ErrorException(Exception inner)
    {
        Message = inner.Message;
        ExceptionMetadata = inner.Data;
    }

    public string Message { get; } = "";
    public IDictionary ExceptionMetadata { get; }
}
