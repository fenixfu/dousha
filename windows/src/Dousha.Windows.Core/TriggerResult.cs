namespace Dousha.Windows.Core;

public sealed record TriggerResult(IReadOnlyList<TriggerCommand> Commands, bool ShouldSuppressKey)
{
    public static TriggerResult None { get; } = new([], ShouldSuppressKey: false);

    public static TriggerResult WithCommand(TriggerCommand command)
    {
        return new TriggerResult([command], ShouldSuppressKey: false);
    }
}
