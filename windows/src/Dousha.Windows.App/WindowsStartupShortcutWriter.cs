using Dousha.Windows.Core;

namespace Dousha.Windows.App;

public sealed class WindowsStartupShortcutWriter : IStartupShortcutWriter
{
    public void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory)
    {
        dynamic shell = CreateShell();
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = workingDirectory;
        shortcut.Save();
    }

    public void DeleteShortcut(string shortcutPath)
    {
        if (File.Exists(shortcutPath))
        {
            File.Delete(shortcutPath);
        }
    }

    public string? ReadShortcutTarget(string shortcutPath)
    {
        if (!File.Exists(shortcutPath))
        {
            return null;
        }

        dynamic shell = CreateShell();
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        return shortcut.TargetPath;
    }

    private static object CreateShell()
    {
        var type = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell is not available.");
        return Activator.CreateInstance(type)
            ?? throw new InvalidOperationException("Failed to create WScript.Shell.");
    }
}
