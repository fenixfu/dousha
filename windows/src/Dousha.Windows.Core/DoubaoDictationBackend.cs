namespace Dousha.Windows.Core;

public sealed class DoubaoDictationBackend : IDictationBackend
{
    private readonly DoubaoCredentialStore _credentialStore;
    private readonly IDoubaoTransportClientFactory _transportClientFactory;
    private readonly Func<IDoubaoOpusEncoder> _encoderFactory;
    private readonly IDiagnosticLog _diagnosticLog;
    private readonly Func<string> _requestIdFactory;

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
        var credentials = await _credentialStore.EnsureCredentialsAsync(cancellationToken);
        var transportClient = await _transportClientFactory.ConnectAsync(cancellationToken);
        await using var transport = new DoubaoAudioTransport(transportClient, _encoderFactory(), _diagnosticLog);
        var transcript = await transport.TranscribeAsync(audio, credentials, _requestIdFactory(), contextHint: "", cancellationToken);
        _diagnosticLog.Lifecycle($"doubao.backend.transcribe_completed textLength={transcript.Length}");
        return transcript;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
