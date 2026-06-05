using Dousha.Windows.Core;
using Xunit;

namespace Dousha.Windows.Tests;

public sealed class CapturedAudioTests
{
    [Fact]
    public void CapturedAudioReportsFormatFramesAndTotalBytes()
    {
        var format = new AudioCaptureFormat(16000, 16, 1);
        var firstFrame = new AudioFrame([1, 2, 3, 4], TimeSpan.FromMilliseconds(10));
        var secondFrame = new AudioFrame([5, 6], TimeSpan.FromMilliseconds(20));

        var audio = new CapturedAudio(format, [firstFrame, secondFrame]);

        Assert.Equal(format, audio.Format);
        Assert.Equal([firstFrame, secondFrame], audio.Frames);
        Assert.Equal(6, audio.ByteCount);
    }
}
