using Dousha.Windows.Core;
using Xunit;

namespace Dousha.Windows.Tests;

public sealed class ApplicationExitCoordinatorTests
{
    [Fact]
    public void RequestExitRaisesExitRequestedOnce()
    {
        var coordinator = new ApplicationExitCoordinator();
        var requests = 0;
        coordinator.ExitRequested += (_, _) => requests++;

        coordinator.RequestExit();
        coordinator.RequestExit();

        Assert.True(coordinator.IsExitRequested);
        Assert.Equal(1, requests);
    }
}
