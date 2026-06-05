using Concentus;
using Concentus.Enums;

namespace Dousha.Windows.Core;

public sealed class ConcentusDoubaoOpusEncoder : IDoubaoOpusEncoder
{
    private const int MaxPacketBytes = 4000;
    private readonly IOpusEncoder _encoder;

    public ConcentusDoubaoOpusEncoder()
    {
        _encoder = OpusCodecFactory.CreateEncoder(
            DoubaoAudioConstants.SampleRateHz,
            DoubaoAudioConstants.ChannelCount,
            OpusApplication.OPUS_APPLICATION_VOIP);
    }

    public byte[] EncodeTenMillisecondFrame(ReadOnlySpan<byte> pcmFrame)
    {
        if (pcmFrame.Length != DoubaoAudioConstants.PcmBytesPerFrame)
        {
            throw new ArgumentException("Doubao Opus encoding requires exactly one 10 ms PCM frame.", nameof(pcmFrame));
        }

        var samples = new short[DoubaoAudioConstants.SamplesPerFrame];
        for (var index = 0; index < samples.Length; index++)
        {
            samples[index] = (short)(pcmFrame[index * 2] | (pcmFrame[(index * 2) + 1] << 8));
        }

        Span<byte> output = stackalloc byte[MaxPacketBytes];
        var byteCount = _encoder.Encode(samples, DoubaoAudioConstants.SamplesPerFrame, output, output.Length);
        return output[..byteCount].ToArray();
    }

    public void Dispose()
    {
    }
}
