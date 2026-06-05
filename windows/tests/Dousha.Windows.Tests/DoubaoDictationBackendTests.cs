using Dousha.Windows.Core;
using Xunit;

namespace Dousha.Windows.Tests;

public sealed class DoubaoDictationBackendTests
{
    [Fact]
    public async Task BackendGetsCredentialsConnectsTransportSendsAudioAndReturnsFinalMandarinTranscript()
    {
        using var workspace = TestWorkspace.Create();
        var paths = WindowsUserDataPaths.Create(workspace.ExecutableDirectory, workspace.UserDataRoot);
        var diagnostics = new RecordingDiagnosticLog();
        var credentialClient = new FakeCredentialClient();
        var credentialStore = new DoubaoCredentialStore(paths, credentialClient, new FixedClock(), diagnostics);
        var transportClient = new ScriptedDoubaoTransportClient([
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("ignored", "TaskStarted", 200, "ok", "")),
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse("ignored", "SessionStarted", 200, "ok", "")),
            DoubaoAsrResponse.Encode(new DoubaoAsrResponse(
                "ignored",
                "TaskResponse",
                200,
                "ok",
                "{\"results\":[{\"text\":\"你好，豆沙。\",\"is_interim\":false,\"is_vad_finished\":true,\"extra\":{\"nonstream_result\":false}}]}"))
        ]);
        var transportFactory = new FakeTransportClientFactory(transportClient);
        var backend = new DoubaoDictationBackend(
            credentialStore,
            transportFactory,
            () => new ConcentusDoubaoOpusEncoder(),
            diagnostics,
            () => "request-10");
        var audio = new CapturedAudio(
            DoubaoAudioConstants.Pcm16KhzMono,
            [new AudioFrame(new byte[DoubaoAudioConstants.PcmBytesPerFrame * 2], TimeSpan.Zero)]);

        var transcript = await backend.TranscribeAsync(audio);

        Assert.Equal("你好，豆沙。", transcript);
        Assert.Equal(1, credentialClient.RegisterCalls);
        Assert.Equal(1, credentialClient.TokenCalls);
        Assert.Equal(1, transportFactory.ConnectCalls);
        Assert.Equal(["StartTask", "StartSession", "TaskRequest", "TaskRequest", "FinishSession"], transportClient.SentMessages.Select(message => DoubaoAsrRequest.Decode(message).MethodName));
        Assert.Contains("doubao.backend.transcribe_started", diagnostics.Joined);
        Assert.Contains("doubao.backend.transcribe_completed textLength=6", diagnostics.Joined);
        Assert.DoesNotContain("token-secret", diagnostics.Joined);
        Assert.DoesNotContain("你好，豆沙。", diagnostics.Joined);
    }

    private sealed class FakeCredentialClient : IDoubaoCredentialClient
    {
        public int RegisterCalls { get; private set; }

        public int TokenCalls { get; private set; }

        public Task<RegisteredDoubaoDevice> RegisterDeviceAsync(CancellationToken cancellationToken = default)
        {
            RegisterCalls++;
            return Task.FromResult(new RegisteredDoubaoDevice("device-1", "install-1", "cdid-1", "open-1", "client-1"));
        }

        public Task<string> FetchTokenAsync(RegisteredDoubaoDevice device, CancellationToken cancellationToken = default)
        {
            TokenCalls++;
            return Task.FromResult("token-secret");
        }
    }

    private sealed class FakeTransportClientFactory(ScriptedDoubaoTransportClient client) : IDoubaoTransportClientFactory
    {
        public int ConnectCalls { get; private set; }

        public Task<IDoubaoTransportClient> ConnectAsync(CancellationToken cancellationToken = default)
        {
            ConnectCalls++;
            return Task.FromResult<IDoubaoTransportClient>(client);
        }
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

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset Now => DateTimeOffset.FromUnixTimeSeconds(2_000_000_000);
    }
}
