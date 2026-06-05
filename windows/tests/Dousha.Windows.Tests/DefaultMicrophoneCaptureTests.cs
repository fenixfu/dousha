using Dousha.Windows.Core;
using Xunit;

namespace Dousha.Windows.Tests;

public sealed class DefaultMicrophoneCaptureTests
{
    [Fact]
    public async Task StopReturnsFramesCapturedAfterStart()
    {
        var format = new AudioCaptureFormat(16000, 16, 1);
        var input = new FakeAudioInput(format);
        await using var capture = new DefaultMicrophoneCapture(input);

        await capture.StartAsync();
        input.Capture([1, 2, 3, 4], TimeSpan.FromMilliseconds(20));
        input.Capture([5, 6], TimeSpan.FromMilliseconds(40));
        var audio = await capture.StopAsync();

        Assert.True(input.Started);
        Assert.True(input.Stopped);
        Assert.Equal(format, audio.Format);
        Assert.Equal(2, audio.Frames.Count);
        Assert.Equal(6, audio.ByteCount);
        Assert.Equal([1, 2, 3, 4], audio.Frames[0].Data.ToArray());
        Assert.Equal(TimeSpan.FromMilliseconds(40), audio.Frames[1].CapturedAt);
    }

    private sealed class FakeAudioInput(AudioCaptureFormat format) : IAudioInput
    {
        public event EventHandler<AudioFrameCapturedEventArgs>? FrameCaptured;

        public AudioCaptureFormat Format { get; } = format;

        public bool Started { get; private set; }

        public bool Stopped { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            Started = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            Stopped = true;
            return Task.CompletedTask;
        }

        public void Capture(byte[] data, TimeSpan capturedAt)
        {
            FrameCaptured?.Invoke(this, new AudioFrameCapturedEventArgs(data, capturedAt));
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
