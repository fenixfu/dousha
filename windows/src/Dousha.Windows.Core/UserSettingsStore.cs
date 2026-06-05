using System.Text.Json;

namespace Dousha.Windows.Core;

public sealed class UserSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly WindowsUserDataPaths _paths;

    public UserSettingsStore(WindowsUserDataPaths paths)
    {
        _paths = paths;
    }

    public UserSettings Load()
    {
        if (!File.Exists(_paths.SettingsFilePath))
        {
            Save(UserSettings.Default);
            return UserSettings.Default;
        }

        var settings = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(_paths.SettingsFilePath), JsonOptions);
        return settings ?? UserSettings.Default;
    }

    public void Save(UserSettings settings)
    {
        Directory.CreateDirectory(_paths.SettingsDirectory);
        File.WriteAllText(_paths.SettingsFilePath, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
