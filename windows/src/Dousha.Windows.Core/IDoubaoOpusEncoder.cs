namespace Dousha.Windows.Core;

public interface IDoubaoOpusEncoder : IDisposable
{
    byte[] EncodeTenMillisecondFrame(ReadOnlySpan<byte> pcmFrame);
}
