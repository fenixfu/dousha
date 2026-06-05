using Dousha.Windows.Core;
using Xunit;

namespace Dousha.Windows.Tests;

public sealed class DoubleTapHoldTriggerTests
{
    private static readonly DateTimeOffset Start = new(2026, 6, 5, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void StartsDictationOnSecondHeldLeftControlWithinTimingWindow()
    {
        var trigger = new DoubleTapHoldTrigger(TriggerSettings.Default);

        var firstDown = trigger.Handle(TriggerKeyEvent.KeyDown(TriggerKey.LeftControl, Start));
        var firstUp = trigger.Handle(TriggerKeyEvent.KeyUp(TriggerKey.LeftControl, Start.AddMilliseconds(60)));
        var secondDown = trigger.Handle(TriggerKeyEvent.KeyDown(TriggerKey.LeftControl, Start.AddMilliseconds(220)));

        Assert.Empty(firstDown.Commands);
        Assert.Empty(firstUp.Commands);
        Assert.Equal([TriggerCommand.StartDictation], secondDown.Commands);
        Assert.False(secondDown.ShouldSuppressKey);
    }

    [Fact]
    public void ReleasingHeldTriggerKeyStopsDictation()
    {
        var trigger = new DoubleTapHoldTrigger(TriggerSettings.Default);

        trigger.Handle(TriggerKeyEvent.KeyDown(TriggerKey.LeftControl, Start));
        trigger.Handle(TriggerKeyEvent.KeyUp(TriggerKey.LeftControl, Start.AddMilliseconds(60)));
        trigger.Handle(TriggerKeyEvent.KeyDown(TriggerKey.LeftControl, Start.AddMilliseconds(220)));
        var release = trigger.Handle(TriggerKeyEvent.KeyUp(TriggerKey.LeftControl, Start.AddMilliseconds(900)));

        Assert.Equal([TriggerCommand.StopDictation], release.Commands);
        Assert.False(release.ShouldSuppressKey);
    }

    [Fact]
    public void SingleControlPressDoesNotStartDictation()
    {
        var trigger = new DoubleTapHoldTrigger(TriggerSettings.Default);

        var down = trigger.Handle(TriggerKeyEvent.KeyDown(TriggerKey.LeftControl, Start));
        var up = trigger.Handle(TriggerKeyEvent.KeyUp(TriggerKey.LeftControl, Start.AddMilliseconds(80)));

        Assert.Empty(down.Commands);
        Assert.Empty(up.Commands);
    }

    [Fact]
    public void SecondPressAfterTimingWindowDoesNotStartDictation()
    {
        var settings = TriggerSettings.Default with { DoubleTapWindowMilliseconds = 250 };
        var trigger = new DoubleTapHoldTrigger(settings);

        trigger.Handle(TriggerKeyEvent.KeyDown(TriggerKey.LeftControl, Start));
        trigger.Handle(TriggerKeyEvent.KeyUp(TriggerKey.LeftControl, Start.AddMilliseconds(60)));
        var lateSecondDown = trigger.Handle(TriggerKeyEvent.KeyDown(TriggerKey.LeftControl, Start.AddMilliseconds(400)));

        Assert.Empty(lateSecondDown.Commands);
        Assert.False(lateSecondDown.ShouldSuppressKey);
    }

    [Fact]
    public void OrdinaryControlShortcutsCancelPendingTapAndAreNotSuppressed()
    {
        var trigger = new DoubleTapHoldTrigger(TriggerSettings.Default);

        var controlDown = trigger.Handle(TriggerKeyEvent.KeyDown(TriggerKey.LeftControl, Start));
        var shortcutKey = trigger.Handle(TriggerKeyEvent.OtherKeyDown(Start.AddMilliseconds(30)));
        var controlUp = trigger.Handle(TriggerKeyEvent.KeyUp(TriggerKey.LeftControl, Start.AddMilliseconds(70)));
        var secondDown = trigger.Handle(TriggerKeyEvent.KeyDown(TriggerKey.LeftControl, Start.AddMilliseconds(160)));

        Assert.Empty(controlDown.Commands);
        Assert.Empty(shortcutKey.Commands);
        Assert.Empty(controlUp.Commands);
        Assert.Empty(secondDown.Commands);
        Assert.False(controlDown.ShouldSuppressKey);
        Assert.False(shortcutKey.ShouldSuppressKey);
        Assert.False(controlUp.ShouldSuppressKey);
        Assert.False(secondDown.ShouldSuppressKey);
    }
}
