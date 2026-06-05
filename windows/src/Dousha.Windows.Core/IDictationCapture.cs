namespace Dousha.Windows.Core;

public interface IDictationCapture : IAsyncDisposable
{
    Task StartAsync(CancellationToken cancellationToken = default);

    Task<CapturedAudio> StopAsync(CancellationToken cancellationToken = default);
}
