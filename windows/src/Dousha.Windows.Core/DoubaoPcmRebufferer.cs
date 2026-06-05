namespace Dousha.Windows.Core;

public sealed record DoubaoPcmFrame(byte[] Pcm, FrameState FrameState);

public static class DoubaoPcmRebufferer
{
    public static IEnumerable<byte[]> ToTenMillisecondFrames(CapturedAudio audio)
    {
        EnsureDoubaoFormat(audio);

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

    public static IEnumerable<DoubaoPcmFrame> ToTenMillisecondFramesWithFinal(CapturedAudio audio)
    {
        EnsureDoubaoFormat(audio);

        var buffer = new List<byte>(DoubaoAudioConstants.PcmBytesPerFrame * 2);
        var sentCompleteFrame = false;
        foreach (var captureFrame in audio.Frames)
        {
            buffer.AddRange(captureFrame.Data.ToArray());
            while (buffer.Count >= DoubaoAudioConstants.PcmBytesPerFrame)
            {
                var frame = buffer.Take(DoubaoAudioConstants.PcmBytesPerFrame).ToArray();
                buffer.RemoveRange(0, DoubaoAudioConstants.PcmBytesPerFrame);
                yield return new DoubaoPcmFrame(frame, sentCompleteFrame ? FrameState.Middle : FrameState.First);
                sentCompleteFrame = true;
            }
        }

        if (buffer.Count > 0)
        {
            var frame = new byte[DoubaoAudioConstants.PcmBytesPerFrame];
            buffer.CopyTo(frame);
            yield return new DoubaoPcmFrame(frame, FrameState.Last);
        }
        else if (sentCompleteFrame)
        {
            yield return new DoubaoPcmFrame(new byte[DoubaoAudioConstants.PcmBytesPerFrame], FrameState.Last);
        }
    }

    private static void EnsureDoubaoFormat(CapturedAudio audio)
    {
        if (audio.Format != DoubaoAudioConstants.Pcm16KhzMono)
        {
            throw new InvalidOperationException("Doubao transport requires 16 kHz mono 16-bit PCM audio.");
        }
    }
}
