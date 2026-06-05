using Dousha.Windows.Core;
using System.Windows.Forms;

namespace Dousha.Windows.App;

public sealed class TrayApplicationLifetime : IDisposable
{
    private readonly ApplicationExitCoordinator _exitCoordinator;
    private readonly TrayApplicationContext _context;

    public TrayApplicationLifetime()
    {
        _exitCoordinator = new ApplicationExitCoordinator();
        _context = new TrayApplicationContext(_exitCoordinator);
    }

    public ApplicationContext Context => _context;

    public void Dispose()
    {
        _context.Dispose();
    }
}
