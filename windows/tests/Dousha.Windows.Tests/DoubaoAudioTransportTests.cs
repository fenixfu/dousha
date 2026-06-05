using Dousha.Windows.Core;
using Xunit;

namespace Dousha.Windows.Tests;

public sealed class DoubaoAudioTransportTests
{
    [Fact]
    public void PcmRebuffererSplitsTwentyMillisecondCaptureBuffersIntoTenMillisecondDoubaoFrames()
    {
        var firstTwentyMsBuffer = Enumerable.Range(0, DoubaoAudioConstants.PcmBytesPerFrame * 2)
            .Select(value => (byte)(value % 251))
            .ToArray();
        var audio = new CapturedAudio(
            DoubaoAudioConstants.Pcm16KhzMono,
            [new AudioFrame(firstTwentyMsBuffer, TimeSpan.Zero)]);

        var frames = DoubaoPcmRebufferer.ToTenMillisecondFrames(audio).ToArray();

        Assert.Equal(2, frames.Length);
        Assert.Equal(firstTwentyMsBuffer[..DoubaoAudioConstants.PcmBytesPerFrame], frames[0]);
        Assert.Equal(firstTwentyMsBuffer[DoubaoAudioConstants.PcmBytesPerFrame..], frames[1]);
    }

    [Fact]
    public void PcmRebuffererCarriesBytesAcrossCaptureBufferBoundaries()
    {
        var firstPartial = Enumerable.Repeat((byte)1, 200).ToArray();
        var secondPartial = Enumerable.Repeat((byte)2, 440).ToArray();
        var audio = new CapturedAudio(
            DoubaoAudioConstants.Pcm16KhzMono,
            [
                new AudioFrame(firstPartial, TimeSpan.Zero),
                new AudioFrame(secondPartial, TimeSpan.FromMilliseconds(20))
            ]);

        var frames = DoubaoPcmRebufferer.ToTenMillisecondFrames(audio).ToArray();

        Assert.Equal(2, frames.Length);
        Assert.Equal(200, frames[0].Count(value => value == 1));
        Assert.Equal(120, frames[0].Count(value => value == 2));
        Assert.All(frames[1], value => Assert.Equal((byte)2, value));
    }

    [Fact]
    public void ConcentusOpusEncoderProducesOpusPacketsForTenMillisecondDoubaoPcmFrames()
    {
        using var encoder = new ConcentusDoubaoOpusEncoder();
        var pcm = new byte[DoubaoAudioConstants.PcmBytesPerFrame];
        for (var sample = 0; sample < DoubaoAudioConstants.SamplesPerFrame; sample++)
        {
            var value = (short)(Math.Sin(sample / 6.0) * short.MaxValue / 10);
            pcm[sample * 2] = (byte)(value & 0xff);
            pcm[(sample * 2) + 1] = (byte)((value >> 8) & 0xff);
        }

        var packet = encoder.EncodeTenMillisecondFrame(pcm);

        Assert.NotEmpty(packet);
        Assert.True(packet.Length < pcm.Length);
    }

    [Fact]
    public async Task TransportSendsEncodedFramesWithDoubaoFrameStatesAndParsesMandarinResponseWithoutLeakingSensitiveData()
    {
        using var encoder = new ConcentusDoubaoOpusEncoder();
        var client = new ScriptedDoubaoTransportClient([
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("request-1", "TaskStarted", 200, "ok", "")),
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("request-1", "SessionStarted", 200, "ok", "")),
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse(
                "request-1",
                "TaskResponse",
                200,
                "ok",
                "{\"results\":[{\"text\":\"你好，豆沙。\",\"is_interim\":false,\"is_vad_finished\":true,\"extra\":{\"nonstream_result\":false}}]}"))
        ]);
        var diagnostics = new RecordingDiagnosticLog();
        var transport = new DoubaoAudioTransport(client, encoder, diagnostics);
        var pcm = new byte[DoubaoAudioConstants.PcmBytesPerFrame * 3];
        for (var index = 0; index < pcm.Length; index++)
        {
            pcm[index] = (byte)(index % 127);
        }

        var transcript = await transport.TranscribeAsync(
            new CapturedAudio(DoubaoAudioConstants.Pcm16KhzMono, [new AudioFrame(pcm, TimeSpan.Zero)]),
            new DoubaoDeviceCredentials("device-1", "install-1", "cdid-1", "open-1", "client-1", "secret-token"),
            "request-1",
            contextHint: "",
            CancellationToken.None);

        var sentRequests = client.SentMessages.Select(message => DoubaoAsrRequest.Decode(message)).ToArray();
        Assert.Equal("你好，豆沙。", transcript);
        Assert.Equal(["StartTask", "StartSession", "TaskRequest", "TaskRequest", "TaskRequest", "FinishSession"], sentRequests.Select(request => request.MethodName));
        Assert.Equal([FrameState.First, FrameState.Middle, FrameState.Last], sentRequests.Where(request => request.MethodName == "TaskRequest").Select(request => request.FrameState));
        Assert.All(sentRequests.Where(request => request.MethodName == "TaskRequest"), request => Assert.NotEmpty(request.AudioData.ToArray()));
        Assert.Contains("doubao.transport.started", diagnostics.Joined);
        Assert.Contains("doubao.transport.audio_frames_sent count=3", diagnostics.Joined);
        Assert.Contains("doubao.transport.finished receivedFinal=True", diagnostics.Joined);
        Assert.DoesNotContain("secret-token", diagnostics.Joined);
        Assert.DoesNotContain("你好，豆沙。", diagnostics.Joined);
    }

    private sealed class ScriptedDoubaoTransportClient : IDoubaoTransportClient
    {
        private readonly Queue<byte[]> _responses;

        public ScriptedDoubaoTransportClient(IEnumerable<byte[]> responses)
        {
            _responses = new Queue<byte[]>(responses);
        }

        public List<byte[]> SentMessages { get; } = [];

        public Task SendAsync(byte[] message, CancellationToken cancellationToken = default)
        {
            SentMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_responses.Dequeue());
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
