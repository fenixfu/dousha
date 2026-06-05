namespace Dousha.Windows.Core;

internal interface IAudioInput : IAsyncDisposable
{
    event EventHandler<AudioFrameCapturedEventArgs> FrameCaptured;

    AudioCaptureFormat Format { get; }

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}

internal sealed class AudioFrameCapturedEventArgs : EventArgs
{
    public AudioFrameCapturedEventArgs(ReadOnlyMemory<byte> data, TimeSpan capturedAt)
    {
        Data = data.ToArray();
        CapturedAt = capturedAt;
    }

    public ReadOnlyMemory<byte> Data { get; }

    public TimeSpan CapturedAt { get; }
}
