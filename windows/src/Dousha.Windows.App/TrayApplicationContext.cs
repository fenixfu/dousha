using Dousha.Windows.Core;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Dousha.Windows.App;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly ApplicationExitCoordinator _exitCoordinator;
    private readonly UserSettingsStore _settingsStore;
    private readonly WindowsUserDataPaths _paths;
    private readonly FileDiagnosticLog _diagnosticLog;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private SettingsWindow? _settingsWindow;

    public TrayApplicationContext(
        ApplicationExitCoordinator exitCoordinator,
        UserSettingsStore settingsStore,
        WindowsUserDataPaths paths,
        FileDiagnosticLog diagnosticLog)
    {
        _exitCoordinator = exitCoordinator;
        _settingsStore = settingsStore;
        _paths = paths;
        _diagnosticLog = diagnosticLog;
        _exitCoordinator.ExitRequested += OnExitRequested;

        _menu = BuildMenu(TrayMenuModel.Create(DictationStatus.Idle));
        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = _menu,
            Icon = SystemIcons.Application,
            Text = TrayMenuText.AppName,
            Visible = true
        };
    }

    protected override void ExitThreadCore()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
        _settingsWindow?.Dispose();
        _exitCoordinator.ExitRequested -= OnExitRequested;

        base.ExitThreadCore();
    }

    private ContextMenuStrip BuildMenu(TrayMenuModel model)
    {
        var menu = new ContextMenuStrip();

        foreach (var item in model.Items)
        {
            var menuItem = new ToolStripMenuItem(item.Text) { Enabled = item.Enabled };
            menuItem.Click += (_, _) => ExecuteCommand(item.Command);
            menu.Items.Add(menuItem);
        }

        return menu;
    }

    private void OnExitRequested(object? sender, EventArgs e)
    {
        _diagnosticLog.Lifecycle("app.exit_requested");
        ExitThread();
    }

    private void ExecuteCommand(TrayMenuCommand? command)
    {
        try
        {
            switch (command)
            {
                case TrayMenuCommand.OpenSettings:
                    OpenSettings();
                    break;
                case TrayMenuCommand.OpenLogsFolder:
                    OpenLogsFolder();
                    break;
                case TrayMenuCommand.Quit:
                    _exitCoordinator.RequestExit();
                    break;
            }
        }
        catch (Exception exception)
        {
            _diagnosticLog.Error(DiagnosticArea.App, "tray_command_failed", exception);
        }
    }

    private void OpenSettings()
    {
        if (_settingsWindow is null || _settingsWindow.IsDisposed)
        {
            _settingsWindow = new SettingsWindow(_settingsStore, _paths);
        }

        _diagnosticLog.Lifecycle("settings.opened");
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void OpenLogsFolder()
    {
        Directory.CreateDirectory(_paths.LogsDirectory);
        _diagnosticLog.Lifecycle("logs_folder.opened");
        Process.Start(new ProcessStartInfo
        {
            FileName = _paths.LogsDirectory,
            UseShellExecute = true
        });
    }
}
