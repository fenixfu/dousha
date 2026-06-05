using Dousha.Windows.Core;
using Xunit;

namespace Dousha.Windows.Tests;

public sealed class DiagnosticLogTests
{
    [Fact]
    public void DiagnosticLogWritesLifecycleStatusAndErrorEvents()
    {
        using var workspace = TestWorkspace.Create();
        var paths = WindowsUserDataPaths.Create(workspace.ExecutableDirectory, workspace.UserDataRoot);
        var logger = new FileDiagnosticLog(paths, new FixedClock(new DateTimeOffset(2026, 6, 5, 8, 0, 0, TimeSpan.Zero)));

        logger.Lifecycle("app.started");
        logger.StatusChanged(DictationStatus.Idle, DictationStatus.Recording);
        logger.Error(DiagnosticArea.Doubao, "websocket_failed", new InvalidOperationException("bad handshake"));

        var contents = File.ReadAllText(paths.CurrentLogFilePath);

        Assert.Contains("lifecycle\tapp.started", contents);
        Assert.Contains("status\tIdle -> Recording", contents);
        Assert.Contains("error\tDoubao\twebsocket_failed\tInvalidOperationException\tmessage_length=13", contents);
    }

    [Fact]
    public void DiagnosticLogDoesNotStoreTranscriptTextOrAudioPayloads()
    {
        using var workspace = TestWorkspace.Create();
        var paths = WindowsUserDataPaths.Create(workspace.ExecutableDirectory, workspace.UserDataRoot);
        var logger = new FileDiagnosticLog(paths, new FixedClock(new DateTimeOffset(2026, 6, 5, 8, 0, 0, TimeSpan.Zero)));
        const string transcript = "完整的中文听写结果不应该进入日志";
        var audioBytes = new byte[] { 1, 2, 3, 4, 5 };

        logger.TranscriptReceived(transcript);
        logger.AudioFrameCaptured(audioBytes.Length);
        logger.Error(DiagnosticArea.Insertion, "insertion_failed", new InvalidOperationException(transcript));

        var contents = File.ReadAllText(paths.CurrentLogFilePath);

        Assert.Contains("transcript.received\tlength=16", contents);
        Assert.Contains("audio.frame_captured\tbytes=5", contents);
        Assert.DoesNotContain(transcript, contents);
        Assert.DoesNotContain(Convert.ToBase64String(audioBytes), contents);
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset Now => now;
    }
}
