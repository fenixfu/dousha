namespace Dousha.Windows.Core;

public sealed record TriggerKeyEvent(
    TriggerKey? Key,
    TriggerKeyEventKind Kind,
    DateTimeOffset Timestamp)
{
    public static TriggerKeyEvent KeyDown(TriggerKey key, DateTimeOffset timestamp)
    {
        return new TriggerKeyEvent(key, TriggerKeyEventKind.KeyDown, timestamp);
    }

    public static TriggerKeyEvent KeyUp(TriggerKey key, DateTimeOffset timestamp)
    {
        return new TriggerKeyEvent(key, TriggerKeyEventKind.KeyUp, timestamp);
    }

    public static TriggerKeyEvent OtherKeyDown(DateTimeOffset timestamp)
    {
        return new TriggerKeyEvent(null, TriggerKeyEventKind.OtherKeyDown, timestamp);
    }
}
