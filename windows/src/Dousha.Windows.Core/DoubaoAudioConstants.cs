namespace Dousha.Windows.Core;

public static class DoubaoAudioConstants
{
    public const int SampleRateHz = 16000;
    public const int ChannelCount = 1;
    public const int BitsPerSample = 16;
    public const int PcmFrameDurationMs = 10;
    public const int SamplesPerFrame = SampleRateHz / (1000 / PcmFrameDurationMs);
    public const int PcmBytesPerFrame = SamplesPerFrame * ChannelCount * (BitsPerSample / 8);

    public static AudioCaptureFormat Pcm16KhzMono { get; } = new(SampleRateHz, BitsPerSample, ChannelCount);
}
