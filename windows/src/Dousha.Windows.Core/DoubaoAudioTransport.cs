namespace Dousha.Windows.Core;

public sealed class DoubaoAudioTransport : IAsyncDisposable
{
    private readonly IDoubaoTransportClient _client;
    private readonly IDoubaoOpusEncoder _encoder;
    private readonly IDiagnosticLog _diagnosticLog;
    private readonly DoubaoAsrResponseParser _responseParser;
    private readonly IClock _clock;
    private readonly TimeSpan _finishTimeout;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly List<byte> _pcmBuffer = [];
    private readonly DoubaoTranscriptAssembler _transcriptAssembler;
    private string _requestId = "";
    private string _token = "";
    private bool _sessionReady;
    private bool _started;
    private bool _sentAnyAudio;
    private int _audioFramesSent;

    public DoubaoAudioTransport(
        IDoubaoTransportClient client,
        IDoubaoOpusEncoder encoder,
        IDiagnosticLog diagnosticLog,
        IClock? clock = null,
        TimeSpan? finishTimeout = null)
    {
        _client = client;
        _encoder = encoder;
        _diagnosticLog = diagnosticLog;
        _responseParser = new DoubaoAsrResponseParser(diagnosticLog);
        _clock = clock ?? SystemClock.Instance;
        _finishTimeout = finishTimeout ?? TimeSpan.FromSeconds(2);
        _transcriptAssembler = new DoubaoTranscriptAssembler(diagnosticLog);
    }

    public async Task<string> TranscribeAsync(
        CapturedAudio audio,
        DoubaoDeviceCredentials credentials,
        string requestId,
        string contextHint,
        CancellationToken cancellationToken = default)
    {
        await StartStreamingAsync(credentials, requestId, contextHint, cancellationToken);
        foreach (var frame in audio.Frames)
        {
            await FeedAudioAsync(frame, cancellationToken);
        }

        return await StopStreamingAsync(cancellationToken);
    }

    public async Task StartStreamingAsync(
        DoubaoDeviceCredentials credentials,
        string requestId,
        string contextHint,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_started)
            {
                return;
            }

            _started = true;
            _requestId = requestId;
            _token = credentials.Token;
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

            _sessionReady = true;
            await FlushCompleteFramesLockedAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task FeedAudioAsync(AudioFrame frame, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _pcmBuffer.AddRange(frame.Data.ToArray());
            if (_sessionReady)
            {
                await FlushCompleteFramesLockedAsync(cancellationToken);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<string> StopStreamingAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_sessionReady)
            {
                await FlushCompleteFramesLockedAsync(cancellationToken);
                await SendLastFrameLockedAsync(cancellationToken);
            }

            _diagnosticLog.Lifecycle($"doubao.transport.audio_frames_sent count={_audioFramesSent}");
            await _client.SendAsync(DoubaoAsrMessageBuilder.FinishSession(_requestId, _token), cancellationToken);
        }
        finally
        {
            _gate.Release();
        }

        while (true)
        {
            byte[] response;
            try
            {
                response = await _client.ReceiveAsync(cancellationToken).WaitAsync(_finishTimeout, cancellationToken);
            }
            catch (TimeoutException)
            {
                _diagnosticLog.Lifecycle($"doubao.transport.finish_timeout timeoutMs={(int)_finishTimeout.TotalMilliseconds} partialLength={_transcriptAssembler.Text.Length}");
                return "";
            }

            var recognitionEvent = _responseParser.Parse(response, "FinishSession");
            if (recognitionEvent.MessageType == "SessionFinished")
            {
                var final = _transcriptAssembler.Text;
                _diagnosticLog.Lifecycle($"doubao.transport.finished receivedFinal=True textLength={final.Length}");
                return final;
            }

            _transcriptAssembler.Apply(recognitionEvent);
            if (recognitionEvent.IsFinalized)
            {
                var final = _transcriptAssembler.Text;
                _diagnosticLog.Lifecycle($"doubao.transport.finished receivedFinal=True textLength={final.Length}");
                return final;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
        _encoder.Dispose();
    }

    private async Task FlushCompleteFramesLockedAsync(CancellationToken cancellationToken)
    {
        while (_pcmBuffer.Count >= DoubaoAudioConstants.PcmBytesPerFrame)
        {
            var frame = _pcmBuffer.Take(DoubaoAudioConstants.PcmBytesPerFrame).ToArray();
            _pcmBuffer.RemoveRange(0, DoubaoAudioConstants.PcmBytesPerFrame);
            var state = _sentAnyAudio ? FrameState.Middle : FrameState.First;
            await SendAudioFrameLockedAsync(frame, state, cancellationToken);
        }
    }

    private async Task SendLastFrameLockedAsync(CancellationToken cancellationToken)
    {
        if (_pcmBuffer.Count > 0)
        {
            var frame = new byte[DoubaoAudioConstants.PcmBytesPerFrame];
            _pcmBuffer.CopyTo(frame);
            _pcmBuffer.Clear();
            await SendAudioFrameLockedAsync(frame, FrameState.Last, cancellationToken);
            return;
        }

        if (_sentAnyAudio)
        {
            await SendAudioFrameLockedAsync(new byte[DoubaoAudioConstants.PcmBytesPerFrame], FrameState.Last, cancellationToken);
        }
    }

    private async Task SendAudioFrameLockedAsync(byte[] pcmFrame, FrameState frameState, CancellationToken cancellationToken)
    {
        var packet = _encoder.EncodeTenMillisecondFrame(pcmFrame);
        await _client.SendAsync(
            DoubaoAsrMessageBuilder.RecognitionFrame(_requestId, packet, frameState, _clock.Now.ToUnixTimeMilliseconds()),
            cancellationToken);
        _sentAnyAudio = true;
        _audioFramesSent++;
    }
}
