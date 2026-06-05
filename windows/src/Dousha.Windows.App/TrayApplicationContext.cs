using Dousha.Windows.Core;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Dousha.Windows.App;

public sealed class TrayApplicationContext : ApplicationContext, IDictationStatusSink
{
    private readonly ApplicationExitCoordinator _exitCoordinator;
    private readonly UserSettingsStore _settingsStore;
    private readonly WindowsUserDataPaths _paths;
    private readonly StartupShortcutService _startupShortcutService;
    private readonly FileDiagnosticLog _diagnosticLog;
    private readonly NotifyIcon _notifyIcon;
    private readonly SynchronizationContext? _uiContext;
    private ContextMenuStrip _menu;
    private SettingsWindow? _settingsWindow;

    public TrayApplicationContext(
        ApplicationExitCoordinator exitCoordinator,
        UserSettingsStore settingsStore,
        WindowsUserDataPaths paths,
        StartupShortcutService startupShortcutService,
        FileDiagnosticLog diagnosticLog)
    {
        _exitCoordinator = exitCoordinator;
        _settingsStore = settingsStore;
        _paths = paths;
        _startupShortcutService = startupShortcutService;
        _diagnosticLog = diagnosticLog;
        _uiContext = SynchronizationContext.Current;
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

    public void StatusChanged(DictationStatus status)
    {
        RunOnUiThread(() =>
        {
            _diagnosticLog.Lifecycle($"tray.status_updated status={status}");
            _notifyIcon.Text = $"{TrayMenuText.AppName} - {TrayStatusFormatter.Format(status)}";
            var previousMenu = _menu;
            _menu = BuildMenu(TrayMenuModel.Create(status));
            _notifyIcon.ContextMenuStrip = _menu;
            previousMenu.Dispose();
        });
    }

    public void ShowNonBlockingError(NonBlockingErrorFeedback feedback)
    {
        RunOnUiThread(() =>
        {
            _notifyIcon.BalloonTipTitle = TrayMenuText.AppName;
            _notifyIcon.BalloonTipText = $"{feedback.Area}: {feedback.EventName}";
            _notifyIcon.ShowBalloonTip(3000);
        });
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
            _settingsWindow = new SettingsWindow(_settingsStore, _paths, _startupShortcutService);
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

    private void RunOnUiThread(Action action)
    {
        if (_uiContext is null)
        {
            action();
            return;
        }

        _uiContext.Post(_ => action(), null);
    }
}
