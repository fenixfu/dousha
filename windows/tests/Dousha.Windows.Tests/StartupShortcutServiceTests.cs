using Dousha.Windows.Core;
using Xunit;

namespace Dousha.Windows.Tests;

public sealed class StartupShortcutServiceTests
{
    [Fact]
    public void EnableCreatesCurrentUserStartupShortcutToPortableExecutable()
    {
        using var workspace = TestWorkspace.Create();
        var startupDirectory = Path.Combine(workspace.UserDataRoot, "Startup");
        var paths = WindowsUserDataPaths.Create(workspace.ExecutableDirectory, workspace.UserDataRoot);
        var writer = new FakeShortcutWriter();
        var service = new StartupShortcutService(paths, writer, startupDirectory);

        service.SetEnabled(true);

        Assert.Equal(Path.Combine(startupDirectory, "Dousha.Windows.lnk"), writer.CreatedShortcutPath);
        Assert.Equal(Path.Combine(paths.ExecutableDirectory, "Dousha.Windows.App.exe"), writer.CreatedTargetPath);
        Assert.Equal(paths.ExecutableDirectory, writer.CreatedWorkingDirectory);
        Assert.DoesNotContain("ProgramData", writer.CreatedShortcutPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DisableDeletesOnlyCurrentUserStartupShortcut()
    {
        using var workspace = TestWorkspace.Create();
        var startupDirectory = Path.Combine(workspace.UserDataRoot, "Startup");
        var paths = WindowsUserDataPaths.Create(workspace.ExecutableDirectory, workspace.UserDataRoot);
        var writer = new FakeShortcutWriter();
        var service = new StartupShortcutService(paths, writer, startupDirectory);

        service.SetEnabled(false);

        Assert.Equal(Path.Combine(startupDirectory, "Dousha.Windows.lnk"), writer.DeletedShortcutPath);
        Assert.Null(writer.CreatedShortcutPath);
    }

    [Fact]
    public void IsEnabledRequiresShortcutToPointAtCurrentPortableExecutable()
    {
        using var workspace = TestWorkspace.Create();
        var startupDirectory = Path.Combine(workspace.UserDataRoot, "Startup");
        var paths = WindowsUserDataPaths.Create(workspace.ExecutableDirectory, workspace.UserDataRoot);
        var writer = new FakeShortcutWriter
        {
            ExistingShortcutTarget = Path.Combine(paths.ExecutableDirectory, "Dousha.Windows.App.exe")
        };
        var service = new StartupShortcutService(paths, writer, startupDirectory);

        Assert.True(service.IsEnabled());

        writer.ExistingShortcutTarget = Path.Combine(workspace.UserDataRoot, "Other.exe");
        Assert.False(service.IsEnabled());
    }

    [Fact]
    public void UserSettingsPersistLaunchAtStartupPreference()
    {
        using var workspace = TestWorkspace.Create();
        var paths = WindowsUserDataPaths.Create(workspace.ExecutableDirectory, workspace.UserDataRoot);
        var store = new UserSettingsStore(paths);

        store.Save(UserSettings.Default with { LaunchAtStartup = true });

        Assert.True(store.Load().LaunchAtStartup);
    }

    private sealed class FakeShortcutWriter : IStartupShortcutWriter
    {
        public string? CreatedShortcutPath { get; private set; }

        public string? CreatedTargetPath { get; private set; }

        public string? CreatedWorkingDirectory { get; private set; }

        public string? DeletedShortcutPath { get; private set; }

        public string? ExistingShortcutTarget { get; set; }

        public void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory)
        {
            CreatedShortcutPath = shortcutPath;
            CreatedTargetPath = targetPath;
            CreatedWorkingDirectory = workingDirectory;
            ExistingShortcutTarget = targetPath;
        }

        public void DeleteShortcut(string shortcutPath)
        {
            DeletedShortcutPath = shortcutPath;
            ExistingShortcutTarget = null;
        }

        public string? ReadShortcutTarget(string shortcutPath)
        {
            return ExistingShortcutTarget;
        }
    }
}
