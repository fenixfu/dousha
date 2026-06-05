namespace Dousha.Windows.Core;

public static class DoubaoPcmRebufferer
{
    public static IEnumerable<byte[]> ToTenMillisecondFrames(CapturedAudio audio)
    {
        if (audio.Format != DoubaoAudioConstants.Pcm16KhzMono)
        {
            throw new InvalidOperationException("Doubao transport requires 16 kHz mono 16-bit PCM audio.");
        }

        var buffer = new List<byte>(DoubaoAudioConstants.PcmBytesPerFrame * 2);
        foreach (var captureFrame in audio.Frames)
        {
            buffer.AddRange(captureFrame.Data.ToArray());
            while (buffer.Count >= DoubaoAudioConstants.PcmBytesPerFrame)
            {
                var frame = buffer.Take(DoubaoAudioConstants.PcmBytesPerFrame).ToArray();
                buffer.RemoveRange(0, DoubaoAudioConstants.PcmBytesPerFrame);
                yield return frame;
            }
        }
    }
}
