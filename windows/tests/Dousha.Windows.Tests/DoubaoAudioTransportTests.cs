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
    public void PcmRebuffererPadsPartialTailAsLastFrame()
    {
        var firstFull = Enumerable.Repeat((byte)1, DoubaoAudioConstants.PcmBytesPerFrame).ToArray();
        var partial = Enumerable.Repeat((byte)2, 40).ToArray();
        var audio = new CapturedAudio(
            DoubaoAudioConstants.Pcm16KhzMono,
            [new AudioFrame([.. firstFull, .. partial], TimeSpan.Zero)]);

        var frames = DoubaoPcmRebufferer.ToTenMillisecondFramesWithFinal(audio).ToArray();

        Assert.Equal(2, frames.Length);
        Assert.Equal(FrameState.First, frames[0].FrameState);
        Assert.Equal(firstFull, frames[0].Pcm);
        Assert.Equal(FrameState.Last, frames[1].FrameState);
        Assert.Equal(DoubaoAudioConstants.PcmBytesPerFrame, frames[1].Pcm.Length);
        Assert.Equal(40, frames[1].Pcm.Count(value => value == 2));
        Assert.Equal(DoubaoAudioConstants.PcmBytesPerFrame - 40, frames[1].Pcm.Count(value => value == 0));
    }

    [Fact]
    public void PcmRebuffererSendsSilentLastFrameAfterCompleteFrames()
    {
        var audio = AudioWithFrames(2);

        var frames = DoubaoPcmRebufferer.ToTenMillisecondFramesWithFinal(audio).ToArray();

        Assert.Equal(3, frames.Length);
        Assert.Equal([FrameState.First, FrameState.Middle, FrameState.Last], frames.Select(frame => frame.FrameState));
        Assert.All(frames.Last().Pcm, value => Assert.Equal((byte)0, value));
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
        var clock = new StepClock(
            DateTimeOffset.FromUnixTimeMilliseconds(1_800_000_000_000),
            TimeSpan.FromMilliseconds(7));
        var client = new ScriptedDoubaoTransportClient([
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("request-1", "TaskStarted", 20000000, "ok", "")),
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("request-1", "SessionStarted", 20000000, "ok", "")),
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse(
                "request-1",
                "TaskResponse",
                20000000,
                "ok",
                "{\"results\":[{\"text\":\"你好，豆沙。\",\"is_interim\":false,\"is_vad_finished\":true,\"extra\":{\"nonstream_result\":false}}]}"))
        ]);
        var diagnostics = new RecordingDiagnosticLog();
        var transport = new DoubaoAudioTransport(client, encoder, diagnostics, clock);
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
        var taskRequests = sentRequests.Where(request => request.MethodName == "TaskRequest").ToArray();
        Assert.Equal("你好，豆沙。", transcript);
        Assert.Equal(["StartTask", "StartSession", "TaskRequest", "TaskRequest", "TaskRequest", "TaskRequest", "FinishSession"], sentRequests.Select(request => request.MethodName));
        Assert.Equal([FrameState.First, FrameState.Middle, FrameState.Middle, FrameState.Last], taskRequests.Select(request => request.FrameState));
        Assert.All(taskRequests, request => Assert.NotEmpty(request.AudioData.ToArray()));
        Assert.Equal([1_800_000_000_000, 1_800_000_000_007, 1_800_000_000_014, 1_800_000_000_021], taskRequests.Select(TimestampMs));
        Assert.All(taskRequests, request => Assert.Contains("timestamp_ms\":1800000000", request.Payload));
        Assert.Contains("\"finish_audio\":true", taskRequests.Last().Payload);
        Assert.Contains("doubao.transport.started", diagnostics.Joined);
        Assert.Contains("doubao.transport.audio_frames_sent count=4", diagnostics.Joined);
        Assert.Contains("doubao.transport.finished receivedFinal=True", diagnostics.Joined);
        Assert.DoesNotContain("secret-token", diagnostics.Joined);
        Assert.DoesNotContain("你好，豆沙。", diagnostics.Joined);
    }

    [Fact]
    public async Task TransportSendsPaddedPartialTailAsOnlyLastFrame()
    {
        using var encoder = new CapturingEncoder();
        var client = new ScriptedDoubaoTransportClient([
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("request-1", "TaskStarted", 20000000, "ok", "")),
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("request-1", "SessionStarted", 20000000, "ok", "")),
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse(
                "request-1",
                "TaskResponse",
                20000000,
                "ok",
                "{\"results\":[{\"text\":\"done\",\"is_interim\":false,\"is_vad_finished\":true,\"extra\":{\"nonstream_result\":false}}]}"))
        ]);
        var transport = new DoubaoAudioTransport(
            client,
            encoder,
            new RecordingDiagnosticLog(),
            new StepClock(DateTimeOffset.FromUnixTimeMilliseconds(1_800_000_000_000), TimeSpan.FromMilliseconds(1)));
        var audio = new CapturedAudio(
            DoubaoAudioConstants.Pcm16KhzMono,
            [new AudioFrame(Enumerable.Repeat((byte)9, 40).ToArray(), TimeSpan.Zero)]);

        await transport.TranscribeAsync(audio, Credentials(), "request-1", contextHint: "", CancellationToken.None);

        var taskRequests = client.SentMessages.Select(message => DoubaoAsrRequest.Decode(message)).Where(request => request.MethodName == "TaskRequest").ToArray();
        Assert.Single(taskRequests);
        Assert.Equal(FrameState.Last, taskRequests[0].FrameState);
        Assert.Contains("\"finish_audio\":true", taskRequests[0].Payload);
        Assert.Single(encoder.PcmFrames);
        Assert.Equal(40, encoder.PcmFrames[0].Count(value => value == 9));
        Assert.Equal(DoubaoAudioConstants.PcmBytesPerFrame - 40, encoder.PcmFrames[0].Count(value => value == 0));
    }

    [Fact]
    public async Task StreamingTransportBuffersFedAudioUntilSessionStartedThenFlushesFrames()
    {
        using var encoder = new CapturingEncoder();
        var client = new ScriptedDoubaoTransportClient([
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("request-1", "TaskStarted", 20000000, "ok", "")),
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("request-1", "SessionStarted", 20000000, "ok", "")),
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("request-1", "SessionFinished", 20000000, "ok", ""))
        ]);
        var transport = new DoubaoAudioTransport(
            client,
            encoder,
            new RecordingDiagnosticLog(),
            new StepClock(DateTimeOffset.FromUnixTimeMilliseconds(1_800_000_000_000), TimeSpan.FromMilliseconds(1)));

        await transport.FeedAudioAsync(new AudioFrame(new byte[DoubaoAudioConstants.PcmBytesPerFrame], TimeSpan.Zero));

        Assert.Empty(client.SentMessages);

        await transport.StartStreamingAsync(Credentials(), "request-1", contextHint: "", CancellationToken.None);

        var startedRequests = client.SentMessages.Select(message => DoubaoAsrRequest.Decode(message)).ToArray();
        Assert.Equal(["StartTask", "StartSession", "TaskRequest"], startedRequests.Select(request => request.MethodName));
        Assert.Equal(FrameState.First, startedRequests.Last().FrameState);

        var transcript = await transport.StopStreamingAsync(CancellationToken.None);

        var sentRequests = client.SentMessages.Select(message => DoubaoAsrRequest.Decode(message)).ToArray();
        Assert.Equal("", transcript);
        Assert.Equal(["StartTask", "StartSession", "TaskRequest", "TaskRequest", "FinishSession"], sentRequests.Select(request => request.MethodName));
        Assert.Equal(FrameState.Last, sentRequests[^2].FrameState);
        Assert.Contains("\"finish_audio\":true", sentRequests[^2].Payload);
    }

    [Fact]
    public async Task StreamingTransportFinishWaitIsBounded()
    {
        using var encoder = new CapturingEncoder();
        var client = new WaitingDoubaoTransportClient([
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("request-1", "TaskStarted", 20000000, "ok", "")),
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("request-1", "SessionStarted", 20000000, "ok", ""))
        ]);
        var transport = new DoubaoAudioTransport(
            client,
            encoder,
            new RecordingDiagnosticLog(),
            new StepClock(DateTimeOffset.FromUnixTimeMilliseconds(1_800_000_000_000), TimeSpan.FromMilliseconds(1)),
            finishTimeout: TimeSpan.FromMilliseconds(20));

        await transport.StartStreamingAsync(Credentials(), "request-1", contextHint: "", CancellationToken.None);
        await transport.FeedAudioAsync(new AudioFrame(new byte[DoubaoAudioConstants.PcmBytesPerFrame], TimeSpan.Zero));

        var transcript = await transport.StopStreamingAsync(CancellationToken.None);

        Assert.Equal("", transcript);
        Assert.Equal("FinishSession", DoubaoAsrRequest.Decode(client.SentMessages.Last()).MethodName);
    }

    [Fact]
    public async Task StreamingTransportAssemblesCommittedSegmentsAndRescuesShrinkingInterimWithoutTranscriptLogs()
    {
        using var encoder = new CapturingEncoder();
        var diagnostics = new RecordingDiagnosticLog();
        var client = new ScriptedDoubaoTransportClient([
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("request-1", "TaskStarted", 20000000, "ok", "")),
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("request-1", "SessionStarted", 20000000, "ok", "")),
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse(
                "request-1",
                "TaskResponse",
                20000000,
                "ok",
                "{\"results\":[{\"text\":\"first long interim\",\"is_interim\":true,\"is_vad_finished\":false}]}")),
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse(
                "request-1",
                "TaskResponse",
                20000000,
                "ok",
                "{\"results\":[{\"text\":\"two\",\"is_interim\":true,\"is_vad_finished\":false}]}")),
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse(
                "request-1",
                "TaskResponse",
                20000000,
                "ok",
                "{\"results\":[{\"text\":\"two final\",\"is_interim\":false,\"is_vad_finished\":true,\"extra\":{\"nonstream_result\":false}}]}")),
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("request-1", "SessionFinished", 20000000, "ok", ""))
        ]);
        var transport = new DoubaoAudioTransport(
            client,
            encoder,
            diagnostics,
            new StepClock(DateTimeOffset.FromUnixTimeMilliseconds(1_800_000_000_000), TimeSpan.FromMilliseconds(1)));

        await transport.StartStreamingAsync(Credentials(), "request-1", contextHint: "", CancellationToken.None);
        var transcript = await transport.StopStreamingAsync(CancellationToken.None);

        Assert.Equal("first long interimtwo final", transcript);
        Assert.Contains("doubao.transcript.segment_rescued textLength=18 newTextLength=3", diagnostics.Joined);
        Assert.Contains("doubao.transcript.segment_final textLength=9 segments=2", diagnostics.Joined);
        Assert.DoesNotContain("first long interim", diagnostics.Joined);
        Assert.DoesNotContain("two final", diagnostics.Joined);
    }

    [Fact]
    public async Task TransportAbortsWithoutAudioFramesWhenStartTaskFails()
    {
        using var encoder = new ConcentusDoubaoOpusEncoder();
        var client = new ScriptedDoubaoTransportClient([
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("request-1", "SessionFailed", 40000000, "", ""))
        ]);
        var diagnostics = new RecordingDiagnosticLog();
        var transport = new DoubaoAudioTransport(client, encoder, diagnostics);

        var exception = await Assert.ThrowsAsync<DoubaoProtocolException>(() => transport.TranscribeAsync(
            AudioWithFrames(3),
            Credentials(),
            "request-1",
            contextHint: "",
            CancellationToken.None));

        var sentRequests = client.SentMessages.Select(message => DoubaoAsrRequest.Decode(message)).ToArray();
        Assert.Equal("StartTask", exception.Phase);
        Assert.Equal(["StartTask"], sentRequests.Select(request => request.MethodName));
        Assert.DoesNotContain("doubao.transport.audio_frames_sent", diagnostics.Joined);
    }

    [Fact]
    public async Task TransportAcceptsCanonicalControlSuccessCodes()
    {
        using var encoder = new ConcentusDoubaoOpusEncoder();
        var client = new ScriptedDoubaoTransportClient([
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("request-1", "TaskStarted", 20000000, "ok", "")),
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("request-1", "SessionStarted", 20000000, "ok", "")),
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse(
                "request-1",
                "TaskResponse",
                20000000,
                "ok",
                "{\"results\":[{\"text\":\"canonical success\",\"is_interim\":false,\"is_vad_finished\":true}]}")),
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("request-1", "SessionFinished", 20000000, "ok", ""))
        ]);
        var diagnostics = new RecordingDiagnosticLog();
        var transport = new DoubaoAudioTransport(client, encoder, diagnostics);

        var transcript = await transport.TranscribeAsync(
            AudioWithFrames(1),
            Credentials(),
            "request-1",
            contextHint: "",
            CancellationToken.None);

        var sentRequests = client.SentMessages.Select(message => DoubaoAsrRequest.Decode(message)).ToArray();
        Assert.Equal("canonical success", transcript);
        Assert.Equal(
            ["StartTask", "StartSession", "TaskRequest", "TaskRequest", "FinishSession"],
            sentRequests.Select(request => request.MethodName));
    }

    [Fact]
    public async Task TransportAbortsWithoutAudioFramesWhenStartTaskReceivesWrongSuccessControlMessage()
    {
        using var encoder = new ConcentusDoubaoOpusEncoder();
        var client = new ScriptedDoubaoTransportClient([
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("request-1", "SessionStarted", 20000000, "ok", ""))
        ]);
        var diagnostics = new RecordingDiagnosticLog();
        var transport = new DoubaoAudioTransport(client, encoder, diagnostics);

        var exception = await Assert.ThrowsAsync<DoubaoProtocolException>(() => transport.TranscribeAsync(
            AudioWithFrames(3),
            Credentials(),
            "request-1",
            contextHint: "",
            CancellationToken.None));

        var sentRequests = client.SentMessages.Select(message => DoubaoAsrRequest.Decode(message)).ToArray();
        Assert.Equal("StartTask", exception.Phase);
        Assert.Equal("SessionStarted", exception.MessageType);
        Assert.Equal(["StartTask"], sentRequests.Select(request => request.MethodName));
        Assert.DoesNotContain("TaskRequest", sentRequests.Select(request => request.MethodName));
        Assert.DoesNotContain("doubao.transport.audio_frames_sent", diagnostics.Joined);
    }

    [Fact]
    public async Task TransportAbortsWithoutAudioFramesWhenStartTaskResponseUsesWrongRequestId()
    {
        using var encoder = new ConcentusDoubaoOpusEncoder();
        var client = new ScriptedDoubaoTransportClient([
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("other-request", "TaskStarted", 20000000, "ok", ""))
        ]);
        var diagnostics = new RecordingDiagnosticLog();
        var transport = new DoubaoAudioTransport(client, encoder, diagnostics);

        var exception = await Assert.ThrowsAsync<DoubaoProtocolException>(() => transport.TranscribeAsync(
            AudioWithFrames(3),
            Credentials(),
            "request-1",
            contextHint: "",
            CancellationToken.None));

        var sentRequests = client.SentMessages.Select(message => DoubaoAsrRequest.Decode(message)).ToArray();
        Assert.Equal("StartTask", exception.Phase);
        Assert.Equal("TaskStarted", exception.MessageType);
        Assert.Equal(["StartTask"], sentRequests.Select(request => request.MethodName));
        Assert.DoesNotContain("other-request", exception.Message);
        Assert.DoesNotContain("doubao.transport.audio_frames_sent", diagnostics.Joined);
    }

    [Fact]
    public async Task TransportAbortsWithoutAudioFramesWhenStartSessionFails()
    {
        using var encoder = new ConcentusDoubaoOpusEncoder();
        var client = new ScriptedDoubaoTransportClient([
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("request-1", "TaskStarted", 20000000, "ok", "")),
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("request-1", "SessionFailed", 40000000, "bad request", ""))
        ]);
        var diagnostics = new RecordingDiagnosticLog();
        var transport = new DoubaoAudioTransport(client, encoder, diagnostics);

        var exception = await Assert.ThrowsAsync<DoubaoProtocolException>(() => transport.TranscribeAsync(
            AudioWithFrames(3),
            Credentials(),
            "request-1",
            contextHint: "",
            CancellationToken.None));

        var sentRequests = client.SentMessages.Select(message => DoubaoAsrRequest.Decode(message)).ToArray();
        Assert.Equal("StartSession", exception.Phase);
        Assert.Equal(["StartTask", "StartSession"], sentRequests.Select(request => request.MethodName));
        Assert.DoesNotContain("TaskRequest", sentRequests.Select(request => request.MethodName));
        Assert.DoesNotContain("doubao.transport.audio_frames_sent", diagnostics.Joined);
        Assert.Contains("statusMessageLength=11", diagnostics.Joined);
        Assert.DoesNotContain("bad request", diagnostics.Joined);
    }

    [Fact]
    public async Task TransportAbortsWithoutAudioFramesWhenStartSessionReceivesWrongSuccessControlMessage()
    {
        using var encoder = new ConcentusDoubaoOpusEncoder();
        var client = new ScriptedDoubaoTransportClient([
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("request-1", "TaskStarted", 20000000, "ok", "")),
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("request-1", "TaskStarted", 20000000, "ok", ""))
        ]);
        var diagnostics = new RecordingDiagnosticLog();
        var transport = new DoubaoAudioTransport(client, encoder, diagnostics);

        var exception = await Assert.ThrowsAsync<DoubaoProtocolException>(() => transport.TranscribeAsync(
            AudioWithFrames(3),
            Credentials(),
            "request-1",
            contextHint: "",
            CancellationToken.None));

        var sentRequests = client.SentMessages.Select(message => DoubaoAsrRequest.Decode(message)).ToArray();
        Assert.Equal("StartSession", exception.Phase);
        Assert.Equal("TaskStarted", exception.MessageType);
        Assert.Equal(["StartTask", "StartSession"], sentRequests.Select(request => request.MethodName));
        Assert.DoesNotContain("TaskRequest", sentRequests.Select(request => request.MethodName));
        Assert.DoesNotContain("doubao.transport.audio_frames_sent", diagnostics.Joined);
    }

    private static CapturedAudio AudioWithFrames(int frameCount)
    {
        var pcm = new byte[DoubaoAudioConstants.PcmBytesPerFrame * frameCount];
        for (var index = 0; index < pcm.Length; index++)
        {
            pcm[index] = (byte)(index % 127);
        }

        return new CapturedAudio(DoubaoAudioConstants.Pcm16KhzMono, [new AudioFrame(pcm, TimeSpan.Zero)]);
    }

    private static DoubaoDeviceCredentials Credentials()
    {
        return new DoubaoDeviceCredentials("device-1", "install-1", "cdid-1", "open-1", "client-1", "secret-token");
    }

    private static long TimestampMs(DoubaoAsrRequest request)
    {
        using var document = System.Text.Json.JsonDocument.Parse(request.Payload);
        return document.RootElement.GetProperty("timestamp_ms").GetInt64();
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

    private sealed class WaitingDoubaoTransportClient : IDoubaoTransportClient
    {
        private readonly Queue<byte[]> _responses;

        public WaitingDoubaoTransportClient(IEnumerable<byte[]> responses)
        {
            _responses = new Queue<byte[]>(responses);
        }

        public List<byte[]> SentMessages { get; } = [];

        public Task SendAsync(byte[] message, CancellationToken cancellationToken = default)
        {
            SentMessages.Add(message);
            return Task.CompletedTask;
        }

        public async Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            if (_responses.Count > 0)
            {
                return _responses.Dequeue();
            }

            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new OperationCanceledException(cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class StepClock(DateTimeOffset initial, TimeSpan step) : IClock
    {
        private DateTimeOffset _next = initial;

        public DateTimeOffset Now
        {
            get
            {
                var current = _next;
                _next = _next.Add(step);
                return current;
            }
        }
    }

    private sealed class CapturingEncoder : IDoubaoOpusEncoder
    {
        public List<byte[]> PcmFrames { get; } = [];

        public byte[] EncodeTenMillisecondFrame(ReadOnlySpan<byte> pcmFrame)
        {
            PcmFrames.Add(pcmFrame.ToArray());
            return [42];
        }

        public void Dispose()
        {
        }
    }
}
