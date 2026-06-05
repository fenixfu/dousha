namespace Dousha.Windows.Core;

public sealed class DoubaoAudioTransport : IAsyncDisposable
{
    private readonly IDoubaoTransportClient _client;
    private readonly IDoubaoOpusEncoder _encoder;
    private readonly IDiagnosticLog _diagnosticLog;
    private readonly DoubaoAsrResponseParser _responseParser;
    private readonly IClock _clock;

    public DoubaoAudioTransport(IDoubaoTransportClient client, IDoubaoOpusEncoder encoder, IDiagnosticLog diagnosticLog, IClock? clock = null)
    {
        _client = client;
        _encoder = encoder;
        _diagnosticLog = diagnosticLog;
        _responseParser = new DoubaoAsrResponseParser(diagnosticLog);
        _clock = clock ?? SystemClock.Instance;
    }

    public async Task<string> TranscribeAsync(
        CapturedAudio audio,
        DoubaoDeviceCredentials credentials,
        string requestId,
        string contextHint,
        CancellationToken cancellationToken = default)
    {
        _diagnosticLog.Lifecycle("doubao.transport.started");

        await _client.SendAsync(DoubaoAsrMessageBuilder.StartTask(requestId, credentials.Token), cancellationToken);
        _responseParser.ParseControl(
            await _client.ReceiveAsync(cancellationToken),
            "StartTask",
            expectedMessageType: "TaskStarted",
            expectedRequestId: requestId);

        var sessionConfig = DoubaoProtocol.BuildSessionConfigJson(credentials.DeviceId, contextHint);
        await _client.SendAsync(DoubaoAsrMessageBuilder.StartSession(requestId, credentials.Token, sessionConfig), cancellationToken);
        _responseParser.ParseControl(
            await _client.ReceiveAsync(cancellationToken),
            "StartSession",
            expectedMessageType: "SessionStarted",
            expectedRequestId: requestId);

        var pcmFrames = DoubaoPcmRebufferer.ToTenMillisecondFramesWithFinal(audio).ToArray();
        foreach (var frame in pcmFrames)
        {
            var packet = _encoder.EncodeTenMillisecondFrame(frame.Pcm);
            await _client.SendAsync(
                DoubaoAsrMessageBuilder.RecognitionFrame(requestId, packet, frame.FrameState, _clock.Now.ToUnixTimeMilliseconds()),
                cancellationToken);
        }

        _diagnosticLog.Lifecycle($"doubao.transport.audio_frames_sent count={pcmFrames.Length}");
        await _client.SendAsync(DoubaoAsrMessageBuilder.FinishSession(requestId, credentials.Token), cancellationToken);

        string transcript = "";
        while (true)
        {
            var recognitionEvent = _responseParser.Parse(await _client.ReceiveAsync(cancellationToken), "FinishSession");
            if (!string.IsNullOrEmpty(recognitionEvent.Text))
            {
                transcript = recognitionEvent.Text;
            }

            if (recognitionEvent.IsFinalized)
            {
                _diagnosticLog.Lifecycle($"doubao.transport.finished receivedFinal=True");
                return transcript;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
        _encoder.Dispose();
    }
}
