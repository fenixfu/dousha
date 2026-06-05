using Dousha.Windows.Core;
using System.Windows.Forms;

namespace Dousha.Windows.App;

public sealed class TrayApplicationLifetime : IDisposable
{
    private readonly ApplicationExitCoordinator _exitCoordinator;
    private readonly FileDiagnosticLog _diagnosticLog;
    private readonly TrayApplicationContext _context;
    private readonly WindowsLowLevelKeyboardHook? _keyboardHook;

    public TrayApplicationLifetime()
    {
        var paths = WindowsUserDataPaths.CreateDefault();
        var settingsStore = new UserSettingsStore(paths);
        _diagnosticLog = new FileDiagnosticLog(paths);
        _exitCoordinator = new ApplicationExitCoordinator();
        var settings = settingsStore.Load();
        _diagnosticLog.Lifecycle("app.started");
        _diagnosticLog.StatusChanged(DictationStatus.Idle, DictationStatus.Idle);
        _context = new TrayApplicationContext(_exitCoordinator, settingsStore, paths, _diagnosticLog);

        try
        {
            _keyboardHook = new WindowsLowLevelKeyboardHook(settings.Trigger, OnTriggerCommand);
            _keyboardHook.Start();
            _diagnosticLog.Lifecycle("trigger.hook_started");
        }
        catch (Exception exception)
        {
            _diagnosticLog.Error(DiagnosticArea.Trigger, "hook_start_failed", exception);
        }
    }

    public ApplicationContext Context => _context;

    public void Dispose()
    {
        _diagnosticLog.Lifecycle("app.disposed");
        _keyboardHook?.Dispose();
        _context.Dispose();
    }

    private void OnTriggerCommand(TriggerCommand command)
    {
        _diagnosticLog.Lifecycle(command switch
        {
            TriggerCommand.StartDictation => "trigger.start_dictation",
            TriggerCommand.StopDictation => "trigger.stop_dictation",
            _ => $"trigger.{command}"
        });
    }
}
