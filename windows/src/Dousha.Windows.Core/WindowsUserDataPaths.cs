namespace Dousha.Windows.Core;

public sealed class WindowsUserDataPaths
{
    private WindowsUserDataPaths(string executableDirectory, string userDataRoot)
    {
        ExecutableDirectory = Path.GetFullPath(executableDirectory);
        UserDataRoot = Path.GetFullPath(userDataRoot);
        SettingsDirectory = Path.Combine(UserDataRoot, "settings");
        LogsDirectory = Path.Combine(UserDataRoot, "logs");
        SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");
        CurrentLogFilePath = Path.Combine(LogsDirectory, "diagnostic.log");
    }

    public string ExecutableDirectory { get; }

    public string UserDataRoot { get; }

    public string SettingsDirectory { get; }

    public string SettingsFilePath { get; }

    public string LogsDirectory { get; }

    public string CurrentLogFilePath { get; }

    public static WindowsUserDataPaths CreateDefault()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Create(AppContext.BaseDirectory, Path.Combine(localAppData, "Dousha", "Windows"));
    }

    public static WindowsUserDataPaths Create(string executableDirectory, string userDataRoot)
    {
        return new WindowsUserDataPaths(executableDirectory, userDataRoot);
    }
}
