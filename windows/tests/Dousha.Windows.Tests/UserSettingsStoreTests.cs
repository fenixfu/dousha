using Dousha.Windows.Core;
using Xunit;

namespace Dousha.Windows.Tests;

public sealed class UserSettingsStoreTests
{
    [Fact]
    public void SettingsAreStoredUnderUserDataRoot()
    {
        using var workspace = TestWorkspace.Create();
        var paths = WindowsUserDataPaths.Create(workspace.ExecutableDirectory, workspace.UserDataRoot);
        var store = new UserSettingsStore(paths);

        store.Save(UserSettings.Default);
        var loaded = store.Load();

        Assert.Equal(UserSettings.Default, loaded);
        Assert.StartsWith(workspace.UserDataRoot, paths.SettingsFilePath, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(workspace.ExecutableDirectory, paths.SettingsFilePath, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(paths.SettingsFilePath));
    }
}
