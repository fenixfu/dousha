namespace Dousha.Windows.Core;

public sealed class StartupShortcutService
{
    private const string ShortcutName = "Dousha.Windows.lnk";
    private const string PortableExecutableName = "Dousha.Windows.App.exe";

    private readonly WindowsUserDataPaths _paths;
    private readonly IStartupShortcutWriter _writer;
    private readonly string _startupDirectory;

    public StartupShortcutService(
        WindowsUserDataPaths paths,
        IStartupShortcutWriter writer,
        string? startupDirectory = null)
    {
        _paths = paths;
        _writer = writer;
        _startupDirectory = startupDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.Startup);
    }

    public string ShortcutPath => Path.Combine(_startupDirectory, ShortcutName);

    public string TargetPath => Path.Combine(_paths.ExecutableDirectory, PortableExecutableName);

    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            Directory.CreateDirectory(_startupDirectory);
            _writer.CreateShortcut(ShortcutPath, TargetPath, _paths.ExecutableDirectory);
            return;
        }

        _writer.DeleteShortcut(ShortcutPath);
    }

    public bool IsEnabled()
    {
        var target = _writer.ReadShortcutTarget(ShortcutPath);
        return string.Equals(
            Path.GetFullPath(target ?? ""),
            Path.GetFullPath(TargetPath),
            StringComparison.OrdinalIgnoreCase);
    }
}
