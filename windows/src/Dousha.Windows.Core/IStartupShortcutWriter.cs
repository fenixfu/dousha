namespace Dousha.Windows.Core;

public interface IStartupShortcutWriter
{
    void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory);

    void DeleteShortcut(string shortcutPath);

    string? ReadShortcutTarget(string shortcutPath);
}
