namespace Dousha.Windows.Core;

public sealed class DictationSessionController
{
    private readonly IDictationCaptureFactory _captureFactory;
    private readonly IDictationBackend _backend;
    private readonly ITextInsertion _insertion;
    private readonly IDictationStatusSink _statusSink;
    private readonly IDiagnosticLog _diagnosticLog;
    private readonly IClock _clock;
    private IDictationCapture? _capture;

    public DictationSessionController(
        IDictationCaptureFactory captureFactory,
        IDictationBackend backend,
        ITextInsertion insertion,
        IDictationStatusSink statusSink,
        IDiagnosticLog diagnosticLog,
        IClock? clock = null)
    {
        _captureFactory = captureFactory;
        _backend = backend;
        _insertion = insertion;
        _statusSink = statusSink;
        _diagnosticLog = diagnosticLog;
        _clock = clock ?? SystemClock.Instance;
    }

    public event Action<NonBlockingErrorFeedback>? NonBlockingError;

    public DictationStatus CurrentStatus { get; private set; } = DictationStatus.Idle;

    public async Task StartRecordingAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentStatus is DictationStatus.Recording)
        {
            return;
        }

        _capture = _captureFactory.Create();
        try
        {
            await _capture.StartAsync(cancellationToken);
            _diagnosticLog.Lifecycle("session.started");
            SetStatus(DictationStatus.Recording);
        }
        catch (Exception exception)
        {
            HandleError(DiagnosticArea.Audio, "capture_start_failed", exception);
            await CleanupAsync();
        }
    }

    public async Task StopAndProcessAsync(CancellationToken cancellationToken = default)
    {
        if (_capture is null || CurrentStatus is not DictationStatus.Recording)
        {
            return;
        }

        try
        {
            var audio = await _capture.StopAsync(cancellationToken);
            _diagnosticLog.AudioFrameCaptured(audio.ByteCount);

            SetStatus(DictationStatus.Transcribing);
            string transcript;
            try
            {
                transcript = await _backend.TranscribeAsync(audio, cancellationToken);
            }
            catch (Exception exception)
            {
                HandleError(DiagnosticArea.Doubao, "transcription_failed", exception);
                return;
            }

            _diagnosticLog.TranscriptReceived(transcript);

            SetStatus(DictationStatus.Inserting);
            try
            {
                await _insertion.InsertAsync(transcript, cancellationToken);
            }
            catch (Exception exception)
            {
                HandleError(DiagnosticArea.Insertion, "insertion_failed", exception);
                return;
            }

            SetStatus(DictationStatus.Success);
        }
        catch (Exception exception)
        {
            HandleError(DiagnosticArea.Audio, "capture_failed", exception);
        }
        finally
        {
            await CleanupAsync();
        }
    }

    public async Task CancelAsync(CancellationToken cancellationToken = default)
    {
        if (_capture is not null && CurrentStatus is DictationStatus.Recording)
        {
            await _capture.StopAsync(cancellationToken);
        }

        _diagnosticLog.Lifecycle("session.cancelled");
        SetStatus(DictationStatus.Idle);
        await CleanupAsync();
    }

    private void HandleError(DiagnosticArea area, string eventName, Exception exception)
    {
        _diagnosticLog.Error(area, eventName, exception);
        NonBlockingError?.Invoke(new NonBlockingErrorFeedback(area, eventName, _clock.Now));
        SetStatus(DictationStatus.Error);
    }

    private void SetStatus(DictationStatus status)
    {
        var previous = CurrentStatus;
        CurrentStatus = status;
        _diagnosticLog.StatusChanged(previous, status);
        _statusSink.StatusChanged(status);
    }

    private async Task CleanupAsync()
    {
        if (_capture is not null)
        {
            await _capture.DisposeAsync();
            _capture = null;
        }

        await _backend.DisposeAsync();
        await _insertion.DisposeAsync();
        _diagnosticLog.Lifecycle("session.cleaned_up");
    }
}
