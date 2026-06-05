namespace Dousha.Windows.Core;

public sealed record UserSettings(string TriggerGesture)
{
    public static UserSettings Default { get; } = new("DoubleTapAndHoldLeftControl");
}
