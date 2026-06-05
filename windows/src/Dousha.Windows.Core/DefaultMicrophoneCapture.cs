namespace Dousha.Windows.Core;

public sealed class DefaultMicrophoneCapture : IDictationCapture
{
    private readonly IAudioInput _input;
    private readonly List<AudioFrame> _frames = [];
    private readonly object _framesLock = new();

    internal DefaultMicrophoneCapture(IAudioInput input)
    {
        _input = input;
    }

    public event EventHandler<AudioFrame>? FrameCaptured;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _input.FrameCaptured += OnFrameCaptured;
        await _input.StartAsync(cancellationToken);
    }

    public async Task<CapturedAudio> StopAsync(CancellationToken cancellationToken = default)
    {
        await _input.StopAsync(cancellationToken);

        lock (_framesLock)
        {
            return new CapturedAudio(_input.Format, _frames);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _input.FrameCaptured -= OnFrameCaptured;
        await _input.DisposeAsync();
    }

    private void OnFrameCaptured(object? sender, AudioFrameCapturedEventArgs e)
    {
        var frame = new AudioFrame(e.Data, e.CapturedAt);
        lock (_framesLock)
        {
            _frames.Add(frame);
        }

        FrameCaptured?.Invoke(this, frame);
    }
}
