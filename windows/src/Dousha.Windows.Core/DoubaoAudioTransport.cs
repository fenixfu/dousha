namespace Dousha.Windows.Core;

public sealed class DoubaoAudioTransport : IAsyncDisposable
{
    private readonly IDoubaoTransportClient _client;
    private readonly IDoubaoOpusEncoder _encoder;
    private readonly IDiagnosticLog _diagnosticLog;
    private readonly DoubaoAsrResponseParser _responseParser;

    public DoubaoAudioTransport(IDoubaoTransportClient client, IDoubaoOpusEncoder encoder, IDiagnosticLog diagnosticLog)
    {
        _client = client;
        _encoder = encoder;
        _diagnosticLog = diagnosticLog;
        _responseParser = new DoubaoAsrResponseParser(diagnosticLog);
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
        _responseParser.Parse(await _client.ReceiveAsync(cancellationToken));

        var sessionConfig = DoubaoProtocol.BuildSessionConfigJson(credentials.DeviceId, contextHint);
        await _client.SendAsync(DoubaoAsrMessageBuilder.StartSession(requestId, credentials.Token, sessionConfig), cancellationToken);
        _responseParser.Parse(await _client.ReceiveAsync(cancellationToken));

        var pcmFrames = DoubaoPcmRebufferer.ToTenMillisecondFrames(audio).ToArray();
        for (var index = 0; index < pcmFrames.Length; index++)
        {
            var state = FrameStateFor(index, pcmFrames.Length);
            var packet = _encoder.EncodeTenMillisecondFrame(pcmFrames[index]);
            await _client.SendAsync(
                DoubaoAsrMessageBuilder.RecognitionFrame(requestId, packet, state, index * DoubaoAudioConstants.PcmFrameDurationMs),
                cancellationToken);
        }

        _diagnosticLog.Lifecycle($"doubao.transport.audio_frames_sent count={pcmFrames.Length}");
        await _client.SendAsync(DoubaoAsrMessageBuilder.FinishSession(requestId, credentials.Token), cancellationToken);

        string transcript = "";
        while (true)
        {
            var recognitionEvent = _responseParser.Parse(await _client.ReceiveAsync(cancellationToken));
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

    private static FrameState FrameStateFor(int index, int count)
    {
        if (count == 1)
        {
            return FrameState.Last;
        }

        if (index == 0)
        {
            return FrameState.First;
        }

        return index == count - 1 ? FrameState.Last : FrameState.Middle;
    }
}
