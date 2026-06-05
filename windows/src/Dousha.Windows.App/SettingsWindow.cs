using Dousha.Windows.Core;
using System.Drawing;
using System.Windows.Forms;

namespace Dousha.Windows.App;

public sealed class SettingsWindow : Form
{
    private readonly UserSettingsStore _settingsStore;
    private readonly TextBox _triggerGestureTextBox;

    public SettingsWindow(UserSettingsStore settingsStore, WindowsUserDataPaths paths)
    {
        _settingsStore = settingsStore;
        var settings = _settingsStore.Load();

        Text = "Dousha Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(420, 170);

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
            Size = new Size(380, 32),
            Text = $"Settings file: {paths.SettingsFilePath}"
        };

        var closeButton = new Button
        {
            DialogResult = DialogResult.OK,
            Location = new Point(304, 126),
            Size = new Size(92, 28),
            Text = "Close"
        };

        AcceptButton = closeButton;
        Controls.AddRange([triggerLabel, _triggerGestureTextBox, locationLabel, closeButton]);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _settingsStore.Save(new UserSettings(_triggerGestureTextBox.Text));
        base.OnFormClosing(e);
    }
}
