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
            item =>
            {
                Assert.Equal(TrayMenuText.QuitCommand, item.Text);
                Assert.True(item.Enabled);
                Assert.Equal(TrayMenuCommand.Quit, item.Command);
            });
    }
}
