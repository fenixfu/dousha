namespace Dousha.Windows.Core;

public interface IDictationCapture : IAsyncDisposable
{
    event EventHandler<AudioFrame>? FrameCaptured;

    Task StartAsync(CancellationToken cancellationToken = default);

    Task<CapturedAudio> StopAsync(CancellationToken cancellationToken = default);
}
