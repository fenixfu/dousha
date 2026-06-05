using Dousha.Windows.Core;
using System.Windows.Forms;

namespace Dousha.Windows.App;

public sealed class TrayApplicationLifetime : IDisposable
{
    private readonly ApplicationExitCoordinator _exitCoordinator;
    private readonly FileDiagnosticLog _diagnosticLog;
    private readonly TrayApplicationContext _context;
    private readonly StartupShortcutService _startupShortcutService;
    private readonly HttpDoubaoCredentialClient _credentialClient;
    private readonly DoubaoCredentialStore _credentialStore;
    private readonly DictationTriggerCommandRunner _dictationRunner;
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
        _startupShortcutService = new StartupShortcutService(paths, new WindowsStartupShortcutWriter());
        _context = new TrayApplicationContext(_exitCoordinator, settingsStore, paths, _startupShortcutService, _diagnosticLog);
        _credentialClient = new HttpDoubaoCredentialClient();
        _credentialStore = new DoubaoCredentialStore(paths, _credentialClient, SystemClock.Instance, _diagnosticLog);
        _dictationRunner = new DictationTriggerCommandRunner(CreateSessionController, _diagnosticLog);
        _dictationRunner.NonBlockingError += _context.ShowNonBlockingError;

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
        _dictationRunner.NonBlockingError -= _context.ShowNonBlockingError;
        _keyboardHook?.Dispose();
        _credentialClient.Dispose();
        _context.Dispose();
    }

    private void OnTriggerCommand(TriggerCommand command)
    {
        _ = HandleTriggerCommandAsync(command);
    }

    private async Task HandleTriggerCommandAsync(TriggerCommand command)
    {
        try
        {
            await _dictationRunner.HandleAsync(command);
        }
        catch (Exception exception)
        {
            _diagnosticLog.Error(DiagnosticArea.Trigger, "trigger_command_failed", exception);
        }
    }

    private DictationSessionController CreateSessionController()
    {
        var backend = new DoubaoDictationBackend(
            _credentialStore,
            new WebSocketDoubaoTransportClientFactory(),
            () => new ConcentusDoubaoOpusEncoder(),
            _diagnosticLog);
        var insertion = new ClipboardPasteInsertion(
            new WindowsClipboardTextWriter(),
            new WindowsPasteCommandSender());
        return new DictationSessionController(
            new DefaultMicrophoneCaptureFactory(),
            backend,
            insertion,
            _context,
            _diagnosticLog);
    }
}
