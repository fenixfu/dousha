namespace Dousha.Windows.Core;

public sealed class TrayMenuModel
{
    private TrayMenuModel(IReadOnlyList<TrayMenuItemDescriptor> items)
    {
        Items = items;
    }

    public IReadOnlyList<TrayMenuItemDescriptor> Items { get; }

    public static TrayMenuModel Create(DictationStatus status)
    {
        return new TrayMenuModel(
        [
            new TrayMenuItemDescriptor(TrayMenuText.AppName, Enabled: false),
            new TrayMenuItemDescriptor($"{TrayMenuText.StatusPrefix}{TrayStatusFormatter.Format(status)}", Enabled: false),
            new TrayMenuItemDescriptor(TrayMenuText.OpenSettingsCommand, Enabled: true, TrayMenuCommand.OpenSettings),
            new TrayMenuItemDescriptor(TrayMenuText.OpenLogsFolderCommand, Enabled: true, TrayMenuCommand.OpenLogsFolder),
            new TrayMenuItemDescriptor(TrayMenuText.QuitCommand, Enabled: true, TrayMenuCommand.Quit)
        ]);
    }
}
