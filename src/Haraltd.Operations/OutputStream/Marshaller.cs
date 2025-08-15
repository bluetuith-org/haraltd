using System.Text.Json;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;

namespace Haraltd.Operations.OutputStream;

internal interface IMarshaller
{
    void Write(OperationToken token, Utf8JsonWriter writer);
}

internal readonly struct ResultMarshaller<T>(T result, bool hasData, bool isError) : IMarshaller
    where T : IResult
{
    void IMarshaller.Write(OperationToken token, Utf8JsonWriter writer)
    {
        writer.WriteStartObject();

        writer.WriteString(
            JsonEncodedKeys.StatusText,
            isError ? JsonEncodedKeys.StatusErrorText : JsonEncodedKeys.StatusOkText
        );

        writer.WriteNumber(JsonEncodedKeys.OperationIdText, token.OperationId);
        writer.WriteNumber(JsonEncodedKeys.RequestIdText, token.RequestId);

        if (hasData)
        {
            if (!isError)
                writer.WriteStartObject(JsonEncodedKeys.DataText);

            result.WriteJsonToStream(writer);

            if (!isError)
                writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }
}

internal readonly struct EventMarshaller<T>(T eventData) : IMarshaller
    where T : IEvent
{
    void IMarshaller.Write(OperationToken token, Utf8JsonWriter writer)
    {
        writer.WriteStartObject();

        writer.WriteNumber(JsonEncodedKeys.EventIdText, eventData.Event.Value);
        writer.WriteString(JsonEncodedKeys.EventActionText, eventData.Action.ToJsonEncodedText());

        writer.WriteStartObject(JsonEncodedKeys.EventText);
        eventData.WriteJsonToStream(writer);
        writer.WriteEndObject();

        writer.WriteEndObject();
    }
}

internal static class JsonEncodedKeys
{
    internal static readonly JsonEncodedText StatusText = JsonEncodedText.Encode("status");
    internal static readonly JsonEncodedText StatusErrorText = JsonEncodedText.Encode("error");
    internal static readonly JsonEncodedText StatusOkText = JsonEncodedText.Encode("ok");

    internal static readonly JsonEncodedText OperationIdText = JsonEncodedText.Encode(
        "operation_id"
    );

    internal static readonly JsonEncodedText RequestIdText = JsonEncodedText.Encode("request_id");
    internal static readonly JsonEncodedText DataText = JsonEncodedText.Encode("data");

    internal static readonly JsonEncodedText EventIdText = JsonEncodedText.Encode("event_id");

    internal static readonly JsonEncodedText EventActionText = JsonEncodedText.Encode(
        "event_action"
    );

    internal static readonly JsonEncodedText EventText = JsonEncodedText.Encode("event");
}
