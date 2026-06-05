using Dousha.Windows.Core;
using Xunit;

namespace Dousha.Windows.Tests;

public sealed class TrayMenuModelTests
{
    [Fact]
    public void IdleTrayMenuShowsAppStatusAndQuitCommand()
    {
        var model = TrayMenuModel.Create(DictationStatus.Idle);

        Assert.Collection(
            model.Items,
            item =>
            {
                Assert.Equal(TrayMenuText.AppName, item.Text);
                Assert.False(item.Enabled);
                Assert.Null(item.Command);
            },
            item =>
            {
                Assert.Equal("Status: Idle", item.Text);
                Assert.False(item.Enabled);
                Assert.Null(item.Command);
            },
            item => AssertCommand(item, TrayMenuText.OpenSettingsCommand, TrayMenuCommand.OpenSettings),
            item => AssertCommand(item, TrayMenuText.OpenLogsFolderCommand, TrayMenuCommand.OpenLogsFolder),
            item => AssertCommand(item, TrayMenuText.QuitCommand, TrayMenuCommand.Quit));
    }

    private static void AssertCommand(TrayMenuItemDescriptor item, string text, TrayMenuCommand command)
    {
        Assert.Equal(text, item.Text);
        Assert.True(item.Enabled);
        Assert.Equal(command, item.Command);
    }
}
