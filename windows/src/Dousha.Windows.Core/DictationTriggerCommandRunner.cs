namespace Dousha.Windows.Core;

public sealed class DictationTriggerCommandRunner
{
    private readonly Func<DictationSessionController> _controllerFactory;
    private readonly IDiagnosticLog _diagnosticLog;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DictationSessionController? _activeController;

    public DictationTriggerCommandRunner(Func<DictationSessionController> controllerFactory, IDiagnosticLog diagnosticLog)
    {
        _controllerFactory = controllerFactory;
        _diagnosticLog = diagnosticLog;
    }

    public event Action<NonBlockingErrorFeedback>? NonBlockingError;

    public async Task HandleAsync(TriggerCommand command, CancellationToken cancellationToken = default)
    {
        _diagnosticLog.Lifecycle($"trigger.command_received command={command}");
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await HandleLockedAsync(command, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task HandleLockedAsync(TriggerCommand command, CancellationToken cancellationToken)
    {
        _diagnosticLog.Lifecycle(command switch
        {
            TriggerCommand.StartDictation => "trigger.start_dictation",
            TriggerCommand.StopDictation => "trigger.stop_dictation",
            _ => $"trigger.{command}"
        });

        if (command is TriggerCommand.StartDictation)
        {
            await StartAsync(cancellationToken);
            return;
        }

        if (command is TriggerCommand.StopDictation)
        {
            await StopAsync(cancellationToken);
        }
    }

    private async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_activeController is not null)
        {
            return;
        }

        _activeController = _controllerFactory();
        _activeController.NonBlockingError += OnNonBlockingError;
        await _activeController.StartRecordingAsync(cancellationToken);
        if (_activeController.CurrentStatus is not DictationStatus.Recording)
        {
            _activeController.NonBlockingError -= OnNonBlockingError;
            _activeController = null;
        }
    }

    private async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_activeController is null)
        {
            return;
        }

        var controller = _activeController;
        _activeController = null;
        try
        {
            await controller.StopAndProcessAsync(cancellationToken);
        }
        finally
        {
            controller.NonBlockingError -= OnNonBlockingError;
        }
    }

    private void OnNonBlockingError(NonBlockingErrorFeedback feedback)
    {
        NonBlockingError?.Invoke(feedback);
    }
}
