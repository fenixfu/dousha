namespace Dousha.Windows.Core;

public sealed class DoubaoDictationBackend : IStreamingDictationBackend
{
    private readonly DoubaoCredentialStore _credentialStore;
    private readonly IDoubaoTransportClientFactory _transportClientFactory;
    private readonly Func<IDoubaoOpusEncoder> _encoderFactory;
    private readonly IDiagnosticLog _diagnosticLog;
    private readonly Func<string> _requestIdFactory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly List<AudioFrame> _pendingFrames = [];
    private DoubaoAudioTransport? _transport;
    private Task? _startupTask;

    public DoubaoDictationBackend(
        DoubaoCredentialStore credentialStore,
        IDoubaoTransportClientFactory transportClientFactory,
        Func<IDoubaoOpusEncoder> encoderFactory,
        IDiagnosticLog diagnosticLog,
        Func<string>? requestIdFactory = null)
    {
        _credentialStore = credentialStore;
        _transportClientFactory = transportClientFactory;
        _encoderFactory = encoderFactory;
        _diagnosticLog = diagnosticLog;
        _requestIdFactory = requestIdFactory ?? (() => Guid.NewGuid().ToString("N"));
    }

    public async Task<string> TranscribeAsync(CapturedAudio audio, CancellationToken cancellationToken = default)
    {
        _diagnosticLog.Lifecycle("doubao.backend.transcribe_started");
        await StartStreamingAsync(cancellationToken);
        foreach (var frame in audio.Frames)
        {
            await FeedAudioAsync(frame, cancellationToken);
        }

        var transcript = await StopStreamingAsync(cancellationToken);
        _diagnosticLog.Lifecycle($"doubao.backend.transcribe_completed textLength={transcript.Length}");
        return transcript;
    }

    public Task StartStreamingAsync(CancellationToken cancellationToken = default)
    {
        if (_startupTask is not null)
        {
            return Task.CompletedTask;
        }

        _diagnosticLog.Lifecycle("doubao.backend.stream_starting");
        _startupTask = StartCoreAsync(cancellationToken);
        return Task.CompletedTask;
    }

    public async Task FeedAudioAsync(AudioFrame frame, CancellationToken cancellationToken = default)
    {
        DoubaoAudioTransport? transport;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            transport = _transport;
            if (transport is null)
            {
                _pendingFrames.Add(frame);
                return;
            }
        }
        finally
        {
            _gate.Release();
        }

        await transport.FeedAudioAsync(frame, cancellationToken);
    }

    public async Task<string> StopStreamingAsync(CancellationToken cancellationToken = default)
    {
        if (_startupTask is null)
        {
            return "";
        }

        await _startupTask.WaitAsync(cancellationToken);
        if (_transport is null)
        {
            return "";
        }

        var transcript = await _transport.StopStreamingAsync(cancellationToken);
        _diagnosticLog.Lifecycle($"doubao.backend.stream_completed textLength={transcript.Length}");
        return transcript;
    }

    public ValueTask DisposeAsync()
    {
        return _transport?.DisposeAsync() ?? ValueTask.CompletedTask;
    }

    private async Task StartCoreAsync(CancellationToken cancellationToken)
    {
        var credentials = await _credentialStore.EnsureCredentialsAsync(cancellationToken);
        var transportClient = await _transportClientFactory.ConnectAsync(credentials.DeviceId, cancellationToken);
        var transport = new DoubaoAudioTransport(transportClient, _encoderFactory(), _diagnosticLog);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _transport = transport;
        }
        finally
        {
            _gate.Release();
        }

        await transport.StartStreamingAsync(credentials, _requestIdFactory(), contextHint: "", cancellationToken);

        AudioFrame[] pending;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            pending = [.. _pendingFrames];
            _pendingFrames.Clear();
        }
        finally
        {
            _gate.Release();
        }

        foreach (var frame in pending)
        {
            await transport.FeedAudioAsync(frame, cancellationToken);
        }
    }
}
