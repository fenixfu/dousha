namespace Dousha.Windows.Core;

public sealed class FileDiagnosticLog : IDiagnosticLog
{
    private readonly WindowsUserDataPaths _paths;
    private readonly IClock _clock;
    private readonly object _writeLock = new();

    public FileDiagnosticLog(WindowsUserDataPaths paths, IClock? clock = null)
    {
        _paths = paths;
        _clock = clock ?? SystemClock.Instance;
    }

    public void Lifecycle(string eventName)
    {
        Write($"lifecycle\t{eventName}");
    }

    public void StatusChanged(DictationStatus from, DictationStatus to)
    {
        Write($"status\t{from} -> {to}");
    }

    public void Error(DiagnosticArea area, string eventName, Exception exception)
    {
        Write($"error\t{area}\t{eventName}\t{exception.GetType().Name}\tmessage_length={exception.Message.Length}");
    }

    public void TranscriptReceived(string transcript)
    {
        Write($"transcript.received\tlength={transcript.Length}");
    }

    public void AudioFrameCaptured(int byteCount)
    {
        Write($"audio.frame_captured\tbytes={byteCount}");
    }

    private void Write(string message)
    {
        lock (_writeLock)
        {
            Directory.CreateDirectory(_paths.LogsDirectory);
            File.AppendAllText(_paths.CurrentLogFilePath, $"{_clock.Now:O}\t{message}{Environment.NewLine}");
        }
    }
}
