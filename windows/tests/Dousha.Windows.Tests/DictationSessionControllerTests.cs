using Dousha.Windows.Core;
using Xunit;

namespace Dousha.Windows.Tests;

public sealed class DictationSessionControllerTests
{
    [Fact]
    public async Task StopAndProcessRunsRecordingTranscribingInsertingSuccessLifecycle()
    {
        var capture = new FakeCapture();
        var backend = new FakeBackend("你好");
        var insertion = new FakeInsertion();
        var statusSink = new FakeStatusSink();
        var logger = new FakeDiagnosticLog();
        var errors = new List<NonBlockingErrorFeedback>();
        var controller = new DictationSessionController(
            new FakeCaptureFactory(capture),
            backend,
            insertion,
            statusSink,
            logger,
            new FixedClock());
        controller.NonBlockingError += errors.Add;

        await controller.StartRecordingAsync();
        await controller.StopAndProcessAsync();

        Assert.Equal(DictationStatus.Success, controller.CurrentStatus);
        Assert.Equal([DictationStatus.Recording, DictationStatus.Transcribing, DictationStatus.Inserting, DictationStatus.Success], statusSink.Statuses);
        Assert.True(capture.Started);
        Assert.True(capture.Stopped);
        Assert.Equal(capture.Audio, backend.Audio);
        Assert.Equal("你好", insertion.InsertedText);
        Assert.Empty(errors);
        Assert.Contains("session.started", logger.LifecycleEvents);
        Assert.Contains("session.cleaned_up", logger.LifecycleEvents);
        Assert.True(capture.Disposed);
        Assert.True(backend.Disposed);
        Assert.True(insertion.Disposed);
    }

    [Fact]
    public async Task BackendErrorSurfacesNonBlockingFeedbackLogsAndCleansUp()
    {
        var capture = new FakeCapture();
        var backend = new FakeBackend("ignored") { Error = new InvalidOperationException("secret transcript should not leak") };
        var insertion = new FakeInsertion();
        var statusSink = new FakeStatusSink();
        var logger = new FakeDiagnosticLog();
        var errors = new List<NonBlockingErrorFeedback>();
        var controller = new DictationSessionController(
            new FakeCaptureFactory(capture),
            backend,
            insertion,
            statusSink,
            logger,
            new FixedClock());
        controller.NonBlockingError += errors.Add;

        await controller.StartRecordingAsync();
        await controller.StopAndProcessAsync();

        Assert.Equal(DictationStatus.Error, controller.CurrentStatus);
        Assert.Equal([DictationStatus.Recording, DictationStatus.Transcribing, DictationStatus.Error], statusSink.Statuses);
        Assert.Single(errors);
        Assert.Equal(DiagnosticArea.Doubao, errors[0].Area);
        Assert.Equal("transcription_failed", errors[0].EventName);
        Assert.Equal(new DateTimeOffset(2026, 6, 5, 8, 0, 0, TimeSpan.Zero), errors[0].OccurredAt);
        Assert.Contains((DiagnosticArea.Doubao, "transcription_failed", "InvalidOperationException"), logger.Errors);
        Assert.True(capture.Disposed);
        Assert.True(backend.Disposed);
        Assert.True(insertion.Disposed);
        Assert.Null(insertion.InsertedText);
    }

    [Fact]
    public async Task CaptureStartErrorSurfacesAudioFeedbackLogsAndCleansUp()
    {
        var capture = new FakeCapture { StartError = new InvalidOperationException("device unavailable") };
        var backend = new FakeBackend("ignored");
        var insertion = new FakeInsertion();
        var statusSink = new FakeStatusSink();
        var logger = new FakeDiagnosticLog();
        var errors = new List<NonBlockingErrorFeedback>();
        var controller = new DictationSessionController(
            new FakeCaptureFactory(capture),
            backend,
            insertion,
            statusSink,
            logger,
            new FixedClock());
        controller.NonBlockingError += errors.Add;

        await controller.StartRecordingAsync();

        Assert.Equal(DictationStatus.Error, controller.CurrentStatus);
        Assert.Equal([DictationStatus.Error], statusSink.Statuses);
        Assert.Single(errors);
        Assert.Equal(DiagnosticArea.Audio, errors[0].Area);
        Assert.Equal("capture_start_failed", errors[0].EventName);
        Assert.Contains((DiagnosticArea.Audio, "capture_start_failed", "InvalidOperationException"), logger.Errors);
        Assert.True(capture.Disposed);
        Assert.True(backend.Disposed);
        Assert.True(insertion.Disposed);
    }

    [Fact]
    public async Task CaptureStopErrorSurfacesAudioFeedbackLogsAndCleansUp()
    {
        var capture = new FakeCapture { StopError = new InvalidOperationException("device disconnected") };
        var backend = new FakeBackend("ignored");
        var insertion = new FakeInsertion();
        var statusSink = new FakeStatusSink();
        var logger = new FakeDiagnosticLog();
        var errors = new List<NonBlockingErrorFeedback>();
        var controller = new DictationSessionController(
            new FakeCaptureFactory(capture),
            backend,
            insertion,
            statusSink,
            logger,
            new FixedClock());
        controller.NonBlockingError += errors.Add;

        await controller.StartRecordingAsync();
        await controller.StopAndProcessAsync();

        Assert.Equal(DictationStatus.Error, controller.CurrentStatus);
        Assert.Equal([DictationStatus.Recording, DictationStatus.Error], statusSink.Statuses);
        Assert.Single(errors);
        Assert.Equal(DiagnosticArea.Audio, errors[0].Area);
        Assert.Equal("capture_failed", errors[0].EventName);
        Assert.Contains((DiagnosticArea.Audio, "capture_failed", "InvalidOperationException"), logger.Errors);
        Assert.Null(backend.Audio);
        Assert.Null(insertion.InsertedText);
        Assert.True(capture.Disposed);
        Assert.True(backend.Disposed);
        Assert.True(insertion.Disposed);
    }


    [Fact]
    public async Task InsertionErrorSurfacesInsertionFeedbackLogsAndCleansUp()
    {
        var capture = new FakeCapture();
        var backend = new FakeBackend("你好");
        var insertion = new FakeInsertion { Error = new InvalidOperationException("target app refused paste") };
        var statusSink = new FakeStatusSink();
        var logger = new FakeDiagnosticLog();
        var errors = new List<NonBlockingErrorFeedback>();
        var controller = new DictationSessionController(
            new FakeCaptureFactory(capture),
            backend,
            insertion,
            statusSink,
            logger,
            new FixedClock());
        controller.NonBlockingError += errors.Add;

        await controller.StartRecordingAsync();
        await controller.StopAndProcessAsync();

        Assert.Equal(DictationStatus.Error, controller.CurrentStatus);
        Assert.Equal([DictationStatus.Recording, DictationStatus.Transcribing, DictationStatus.Inserting, DictationStatus.Error], statusSink.Statuses);
        Assert.Single(errors);
        Assert.Equal(DiagnosticArea.Insertion, errors[0].Area);
        Assert.Equal("insertion_failed", errors[0].EventName);
        Assert.Contains((DiagnosticArea.Insertion, "insertion_failed", "InvalidOperationException"), logger.Errors);
        Assert.True(capture.Disposed);
        Assert.True(backend.Disposed);
        Assert.True(insertion.Disposed);
    }

    [Fact]
    public async Task CancelStopsActiveCaptureReturnsIdleAndCleansUp()
    {
        var capture = new FakeCapture();
        var backend = new FakeBackend("ignored");
        var insertion = new FakeInsertion();
        var statusSink = new FakeStatusSink();
        var logger = new FakeDiagnosticLog();
        var controller = new DictationSessionController(
            new FakeCaptureFactory(capture),
            backend,
            insertion,
            statusSink,
            logger,
            new FixedClock());

        await controller.StartRecordingAsync();
        await controller.CancelAsync();

        Assert.Equal(DictationStatus.Idle, controller.CurrentStatus);
        Assert.Equal([DictationStatus.Recording, DictationStatus.Idle], statusSink.Statuses);
        Assert.True(capture.Stopped);
        Assert.Null(backend.Audio);
        Assert.Null(insertion.InsertedText);
        Assert.Contains("session.cancelled", logger.LifecycleEvents);
        Assert.True(capture.Disposed);
        Assert.True(backend.Disposed);
        Assert.True(insertion.Disposed);
    }

    private sealed class FakeCaptureFactory(FakeCapture capture) : IDictationCaptureFactory
    {
        public IDictationCapture Create()
        {
            return capture;
        }
    }

    private sealed class FakeCapture : IDictationCapture
    {
        public CapturedAudio Audio { get; } = new(
            new AudioCaptureFormat(16000, 16, 1),
            [new AudioFrame([1, 2, 3, 4], TimeSpan.Zero), new AudioFrame([5, 6, 7, 8, 9, 10, 11, 12], TimeSpan.FromMilliseconds(20))]);

        public bool Started { get; private set; }

        public Exception? StartError { get; init; }

        public Exception? StopError { get; init; }

        public bool Stopped { get; private set; }

        public bool Disposed { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            Started = true;
            return StartError is null
                ? Task.CompletedTask
                : Task.FromException(StartError);
        }

        public Task<CapturedAudio> StopAsync(CancellationToken cancellationToken = default)
        {
            Stopped = true;
            return StopError is null
                ? Task.FromResult(Audio)
                : Task.FromException<CapturedAudio>(StopError);
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeBackend(string transcript) : IDictationBackend
    {
        public CapturedAudio? Audio { get; private set; }

        public Exception? Error { get; init; }

        public bool Disposed { get; private set; }

        public Task<string> TranscribeAsync(CapturedAudio audio, CancellationToken cancellationToken = default)
        {
            Audio = audio;
            return Error is null
                ? Task.FromResult(transcript)
                : Task.FromException<string>(Error);
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeInsertion : ITextInsertion
    {
        public string? InsertedText { get; private set; }

        public Exception? Error { get; init; }

        public bool Disposed { get; private set; }

        public Task InsertAsync(string text, CancellationToken cancellationToken = default)
        {
            InsertedText = text;
            return Error is null
                ? Task.CompletedTask
                : Task.FromException(Error);
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeStatusSink : IDictationStatusSink
    {
        public List<DictationStatus> Statuses { get; } = [];

        public void StatusChanged(DictationStatus status)
        {
            Statuses.Add(status);
        }
    }

    private sealed class FakeDiagnosticLog : IDiagnosticLog
    {
        public List<string> LifecycleEvents { get; } = [];

        public List<(DiagnosticArea Area, string EventName, string ExceptionType)> Errors { get; } = [];

        public void Lifecycle(string eventName)
        {
            LifecycleEvents.Add(eventName);
        }

        public void StatusChanged(DictationStatus from, DictationStatus to)
        {
        }

        public void Error(DiagnosticArea area, string eventName, Exception exception)
        {
            Errors.Add((area, eventName, exception.GetType().Name));
        }

        public void TranscriptReceived(string transcript)
        {
        }

        public void AudioFrameCaptured(int byteCount)
        {
        }
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset Now => new(2026, 6, 5, 8, 0, 0, TimeSpan.Zero);
    }
}
