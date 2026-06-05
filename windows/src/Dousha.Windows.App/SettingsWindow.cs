using Dousha.Windows.Core;
using System.Drawing;
using System.Windows.Forms;

namespace Dousha.Windows.App;

public sealed class SettingsWindow : Form
{
    private readonly UserSettingsStore _settingsStore;
    private readonly StartupShortcutService _startupShortcutService;
    private readonly TextBox _triggerGestureTextBox;
    private readonly CheckBox _launchAtStartupCheckBox;

    public SettingsWindow(
        UserSettingsStore settingsStore,
        WindowsUserDataPaths paths,
        StartupShortcutService startupShortcutService)
    {
        _settingsStore = settingsStore;
        _startupShortcutService = startupShortcutService;
        var settings = _settingsStore.Load();

        Text = "Dousha Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(420, 206);

        var triggerLabel = new Label
        {
            AutoSize = true,
            Location = new Point(16, 18),
            Text = "Default trigger"
        };

        _triggerGestureTextBox = new TextBox
        {
            Location = new Point(16, 42),
            ReadOnly = true,
            Size = new Size(380, 23),
            Text = settings.TriggerGesture
        };

        var locationLabel = new Label
        {
            AutoEllipsis = true,
            Location = new Point(16, 82),
            Size = new Size(380, 24),
            Text = $"Settings file: {paths.SettingsFilePath}"
        };

        var timingLabel = new Label
        {
            AutoSize = true,
            Location = new Point(16, 108),
            Text = $"Double-tap window: {settings.Trigger.DoubleTapWindowMilliseconds} ms"
        };

        _launchAtStartupCheckBox = new CheckBox
        {
            AutoSize = true,
            Checked = settings.LaunchAtStartup && _startupShortcutService.IsEnabled(),
            Location = new Point(16, 136),
            Text = "Launch at Windows sign-in"
        };

        var closeButton = new Button
        {
            DialogResult = DialogResult.OK,
            Location = new Point(304, 162),
            Size = new Size(92, 28),
            Text = "Close"
        };

        AcceptButton = closeButton;
        Controls.AddRange([triggerLabel, _triggerGestureTextBox, locationLabel, timingLabel, _launchAtStartupCheckBox, closeButton]);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _settingsStore.Save(_settingsStore.Load() with
        {
            TriggerGesture = _triggerGestureTextBox.Text,
            LaunchAtStartup = _launchAtStartupCheckBox.Checked
        });
        _startupShortcutService.SetEnabled(_launchAtStartupCheckBox.Checked);
        base.OnFormClosing(e);
    }
}
