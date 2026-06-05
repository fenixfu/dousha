using Dousha.Windows.Core;
using System.Drawing;
using System.Windows.Forms;

namespace Dousha.Windows.App;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly ApplicationExitCoordinator _exitCoordinator;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;

    public TrayApplicationContext(ApplicationExitCoordinator exitCoordinator)
    {
        _exitCoordinator = exitCoordinator;
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
        _exitCoordinator.ExitRequested -= OnExitRequested;

        base.ExitThreadCore();
    }

    private ContextMenuStrip BuildMenu(TrayMenuModel model)
    {
        var menu = new ContextMenuStrip();

        foreach (var item in model.Items)
        {
            if (item.Command is TrayMenuCommand.Quit)
            {
                var quitItem = new ToolStripMenuItem(item.Text) { Enabled = item.Enabled };
                quitItem.Click += (_, _) => _exitCoordinator.RequestExit();
                menu.Items.Add(quitItem);
                continue;
            }

            menu.Items.Add(new ToolStripMenuItem(item.Text) { Enabled = item.Enabled });
        }

        return menu;
    }

    private void OnExitRequested(object? sender, EventArgs e)
    {
        ExitThread();
    }
}
