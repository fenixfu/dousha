namespace Dousha.Windows.Core;

public interface IDictationBackend : IAsyncDisposable
{
    Task<string> TranscribeAsync(CapturedAudio audio, CancellationToken cancellationToken = default);
}

public interface IStreamingDictationBackend : IDictationBackend
{
    Task StartStreamingAsync(CancellationToken cancellationToken = default);

    Task FeedAudioAsync(AudioFrame frame, CancellationToken cancellationToken = default);

    Task<string> StopStreamingAsync(CancellationToken cancellationToken = default);
}
