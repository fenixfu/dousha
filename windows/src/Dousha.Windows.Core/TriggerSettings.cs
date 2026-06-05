namespace Dousha.Windows.Core;

public sealed record TriggerSettings(
    TriggerKey TriggerKey,
    int DoubleTapWindowMilliseconds)
{
    public static TriggerSettings Default { get; } = new(
        TriggerKey.LeftControl,
        DoubleTapWindowMilliseconds: 300);
}
