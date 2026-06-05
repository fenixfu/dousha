using Dousha.Windows.Core;
using Xunit;

namespace Dousha.Windows.Tests;

public sealed class DictationTriggerCommandRunnerTests
{
    [Fact]
    public async Task TriggerStartAndStopRunsFullDictationSessionThroughInsertionAndStatus()
    {
        var capture = new FakeCapture();
        var backend = new FakeBackend("你好，豆沙。");
        var insertion = new FakeInsertion();
        var statusSink = new FakeStatusSink();
        var diagnostics = new RecordingDiagnosticLog();
        var runner = new DictationTriggerCommandRunner(
            () => new DictationSessionController(
                new FakeCaptureFactory(capture),
                backend,
                insertion,
                statusSink,
                diagnostics,
                new FixedClock()),
            diagnostics);

        await runner.HandleAsync(TriggerCommand.StartDictation);
        await runner.HandleAsync(TriggerCommand.StopDictation);

        Assert.True(capture.Started);
        Assert.True(capture.Stopped);
        Assert.Equal(capture.Audio, backend.Audio);
        Assert.Equal("你好，豆沙。", insertion.InsertedText);
        Assert.Equal([DictationStatus.Recording, DictationStatus.Transcribing, DictationStatus.Inserting, DictationStatus.Success], statusSink.Statuses);
        Assert.Contains("trigger.start_dictation", diagnostics.Joined);
        Assert.Contains("trigger.stop_dictation", diagnostics.Joined);
    }

    [Fact]
    public async Task TriggerBackendFailureSurfacesNonBlockingFeedbackThroughRunner()
    {
        var capture = new FakeCapture();
        var backend = new FakeBackend("ignored") { Error = new InvalidOperationException("token secret") };
        var diagnostics = new RecordingDiagnosticLog();
        var feedback = new List<NonBlockingErrorFeedback>();
        var runner = new DictationTriggerCommandRunner(
            () => new DictationSessionController(
                new FakeCaptureFactory(capture),
                backend,
                new FakeInsertion(),
                new FakeStatusSink(),
                diagnostics,
                new FixedClock()),
            diagnostics);
        runner.NonBlockingError += feedback.Add;

        await runner.HandleAsync(TriggerCommand.StartDictation);
        await runner.HandleAsync(TriggerCommand.StopDictation);

        Assert.Single(feedback);
        Assert.Equal(DiagnosticArea.Doubao, feedback[0].Area);
        Assert.Equal("transcription_failed", feedback[0].EventName);
        Assert.Contains(("Doubao", "transcription_failed", "InvalidOperationException"), diagnostics.Errors);
        Assert.DoesNotContain("token secret", diagnostics.Joined);
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
            DoubaoAudioConstants.Pcm16KhzMono,
            [new AudioFrame(new byte[DoubaoAudioConstants.PcmBytesPerFrame], TimeSpan.Zero)]);

        public bool Started { get; private set; }

        public bool Stopped { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            Started = true;
            return Task.CompletedTask;
        }

        public Task<CapturedAudio> StopAsync(CancellationToken cancellationToken = default)
        {
            Stopped = true;
            return Task.FromResult(Audio);
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeBackend(string transcript) : IDictationBackend
    {
        public CapturedAudio? Audio { get; private set; }

        public Exception? Error { get; init; }

        public Task<string> TranscribeAsync(CapturedAudio audio, CancellationToken cancellationToken = default)
        {
            Audio = audio;
            return Error is null ? Task.FromResult(transcript) : Task.FromException<string>(Error);
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeInsertion : ITextInsertion
    {
        public string? InsertedText { get; private set; }

        public Task InsertAsync(string text, CancellationToken cancellationToken = default)
        {
            InsertedText = text;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
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

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset Now => new(2026, 6, 5, 8, 0, 0, TimeSpan.Zero);
    }
}
