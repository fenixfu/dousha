namespace Dousha.Windows.Core;

public interface IDiagnosticLog
{
    void Lifecycle(string eventName);

    void StatusChanged(DictationStatus from, DictationStatus to);

    void Error(DiagnosticArea area, string eventName, Exception exception);

    void TranscriptReceived(string transcript);

    void AudioFrameCaptured(int byteCount);
}
