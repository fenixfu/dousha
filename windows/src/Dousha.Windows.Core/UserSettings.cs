namespace Dousha.Windows.Core;

public sealed record UserSettings
{
    public static UserSettings Default { get; } = new();

    public string TriggerGesture { get; init; } = "DoubleTapAndHoldLeftControl";

    public TriggerSettings Trigger { get; init; } = TriggerSettings.Default;
}
