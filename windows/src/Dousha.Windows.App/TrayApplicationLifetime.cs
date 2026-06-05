using Dousha.Windows.Core;
using System.Windows.Forms;

namespace Dousha.Windows.App;

public sealed class TrayApplicationLifetime : IDisposable
{
    private readonly ApplicationExitCoordinator _exitCoordinator;
    private readonly FileDiagnosticLog _diagnosticLog;
    private readonly TrayApplicationContext _context;

    public TrayApplicationLifetime()
    {
        var paths = WindowsUserDataPaths.CreateDefault();
        var settingsStore = new UserSettingsStore(paths);
        _diagnosticLog = new FileDiagnosticLog(paths);
        _exitCoordinator = new ApplicationExitCoordinator();
        settingsStore.Load();
        _diagnosticLog.Lifecycle("app.started");
        _diagnosticLog.StatusChanged(DictationStatus.Idle, DictationStatus.Idle);
        _context = new TrayApplicationContext(_exitCoordinator, settingsStore, paths, _diagnosticLog);
    }

    public ApplicationContext Context => _context;

    public void Dispose()
    {
        _diagnosticLog.Lifecycle("app.disposed");
        _context.Dispose();
    }
}
