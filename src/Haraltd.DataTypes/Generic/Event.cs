using System.Text.Json;

namespace Haraltd.DataTypes.Generic;

public interface IEvent : IResult
{
    public enum EventAction
    {
        None = 0,
        Added,
        Updated,
        Removed,
    }

    public EventType Event { get; }
    public EventAction Action { get; set; }
}

public static class ActionEnumExtensions
{
    private static readonly JsonEncodedText NoneAction = JsonEncodedText.Encode("none");
    private static readonly JsonEncodedText AddedAction = JsonEncodedText.Encode("added");
    private static readonly JsonEncodedText UpdatedAction = JsonEncodedText.Encode("updated");
    private static readonly JsonEncodedText RemovedAction = JsonEncodedText.Encode("removed");

    public static JsonEncodedText ToJsonEncodedText(this IEvent.EventAction action)
    {
        return action switch
        {
            IEvent.EventAction.Added => AddedAction,
            IEvent.EventAction.Updated => UpdatedAction,
            IEvent.EventAction.Removed => RemovedAction,
            _ => NoneAction,
        };
    }
}

public readonly record struct EventType
{
    public EventType(byte code)
    {
        Value = code;
    }

    public byte Value { get; }
}

public partial class EventTypes
{
    protected const byte None = 0,
        Error = 1;

    public static readonly EventType EventNone = new(None);
    public static readonly EventType EventError = new(Error);
}
