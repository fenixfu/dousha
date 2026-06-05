namespace Dousha.Windows.Core;

public interface IDictationBackend : IAsyncDisposable
{
    Task<string> TranscribeAsync(CapturedAudio audio, CancellationToken cancellationToken = default);
}
