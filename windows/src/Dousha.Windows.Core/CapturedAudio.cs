namespace Dousha.Windows.Core;

public sealed record AudioCaptureFormat(int SampleRateHz, int BitsPerSample, int ChannelCount);

public sealed class AudioFrame
{
    private readonly byte[] _data;

    public AudioFrame(byte[] data, TimeSpan capturedAt)
        : this((ReadOnlyMemory<byte>)data, capturedAt)
    {
    }

    public AudioFrame(ReadOnlyMemory<byte> data, TimeSpan capturedAt)
    {
        _data = data.ToArray();
        CapturedAt = capturedAt;
    }

    public ReadOnlyMemory<byte> Data => _data;

    public TimeSpan CapturedAt { get; }

    public int ByteCount => _data.Length;
}

public sealed class CapturedAudio
{
    public CapturedAudio(AudioCaptureFormat format, IEnumerable<AudioFrame> frames)
    {
        Format = format;
        Frames = frames.ToArray();
    }

    public AudioCaptureFormat Format { get; }

    public IReadOnlyList<AudioFrame> Frames { get; }

    public int ByteCount => Frames.Sum(frame => frame.ByteCount);
}
