using Dousha.Windows.Core;
using Xunit;

namespace Dousha.Windows.Tests;

public sealed class TriggerSettingsPersistenceTests
{
    [Fact]
    public void TriggerKeyAndTimingSettingsCanBePersistedAndLoaded()
    {
        using var workspace = TestWorkspace.Create();
        var paths = WindowsUserDataPaths.Create(workspace.ExecutableDirectory, workspace.UserDataRoot);
        var store = new UserSettingsStore(paths);
        var settings = UserSettings.Default with
        {
            Trigger = new TriggerSettings(
                TriggerKey.LeftControl,
                DoubleTapWindowMilliseconds: 375)
        };

        store.Save(settings);
        var loaded = store.Load();

        Assert.Equal(TriggerKey.LeftControl, loaded.Trigger.TriggerKey);
        Assert.Equal(375, loaded.Trigger.DoubleTapWindowMilliseconds);
    }
}
