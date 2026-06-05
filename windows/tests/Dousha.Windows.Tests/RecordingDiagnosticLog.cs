using Dousha.Windows.Core;

namespace Dousha.Windows.Tests;

internal sealed class RecordingDiagnosticLog : IDiagnosticLog
{
    public List<string> LifecycleEvents { get; } = [];

    public List<(string Area, string EventName, string ExceptionType)> Errors { get; } = [];

    public string Joined => string.Join("\n", LifecycleEvents.Concat(Errors.Select(error => $"{error.Area}:{error.EventName}:{error.ExceptionType}")));

    public void Lifecycle(string eventName)
    {
        LifecycleEvents.Add(eventName);
    }

    public void StatusChanged(DictationStatus from, DictationStatus to)
    {
    }

    public void Error(DiagnosticArea area, string eventName, Exception exception)
    {
        Errors.Add((area.ToString(), eventName, exception.GetType().Name));
    }

    public void TranscriptReceived(string transcript)
    {
        Lifecycle($"transcript.length={transcript.Length}");
    }

    public void AudioFrameCaptured(int byteCount)
    {
    }
}
