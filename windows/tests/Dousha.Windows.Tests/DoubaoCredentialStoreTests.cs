using Dousha.Windows.Core;
using System.Text.Json;
using Xunit;

namespace Dousha.Windows.Tests;

public sealed class DoubaoCredentialStoreTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(2_000_000_000);

    [Fact]
    public async Task CacheMissRegistersDeviceFetchesTokenAndPersistsInUserData()
    {
        using var workspace = TestWorkspace.Create();
        var paths = WindowsUserDataPaths.Create(workspace.ExecutableDirectory, workspace.UserDataRoot);
        var client = new FakeDoubaoCredentialClient();
        var logger = new RecordingDiagnosticLog();
        var store = new DoubaoCredentialStore(paths, client, new FixedClock(Now), logger);

        var credentials = await store.EnsureCredentialsAsync();

        Assert.Equal("device-1", credentials.DeviceId);
        Assert.Equal("token-valid", credentials.Token);
        Assert.Equal(1, client.RegisterCalls);
        Assert.Equal(1, client.TokenCalls);
        Assert.StartsWith(workspace.UserDataRoot, paths.DoubaoCredentialsFilePath, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(workspace.ExecutableDirectory, paths.DoubaoCredentialsFilePath, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(paths.DoubaoCredentialsFilePath));
        Assert.Contains("doubao.credentials.cache_miss", logger.LifecycleEvents);
        Assert.DoesNotContain("token-valid", logger.Joined);
    }

    [Fact]
    public async Task CacheHitReturnsPersistedCredentialsWithoutNetworkCalls()
    {
        using var workspace = TestWorkspace.Create();
        var paths = WindowsUserDataPaths.Create(workspace.ExecutableDirectory, workspace.UserDataRoot);
        var expected = Fixtures.Credentials(token: Fixtures.JwtExpiringAt(Now.AddHours(2)));
        await new PlainDoubaoCredentialCache(paths).SaveAsync(expected);
        var client = new FakeDoubaoCredentialClient();
        var store = new DoubaoCredentialStore(paths, client, new FixedClock(Now), new RecordingDiagnosticLog());

        var credentials = await store.EnsureCredentialsAsync();

        Assert.Equal(expected, credentials);
        Assert.Equal(0, client.RegisterCalls);
        Assert.Equal(0, client.TokenCalls);
    }

    [Fact]
    public async Task PreParityIdentityCacheRegistersAndPersistsCanonicalReplacement()
    {
        using var workspace = TestWorkspace.Create();
        var paths = WindowsUserDataPaths.Create(workspace.ExecutableDirectory, workspace.UserDataRoot);
        var stale = Fixtures.Credentials(token: Fixtures.JwtExpiringAt(Now.AddHours(2))) with
        {
            Openudid = "00112233445566778899aabbccddeeff",
            Clientudid = "11111111222233334444555555555555"
        };
        await new PlainDoubaoCredentialCache(paths).SaveAsync(stale);
        var client = new FakeDoubaoCredentialClient();
        var logger = new RecordingDiagnosticLog();
        var store = new DoubaoCredentialStore(paths, client, new FixedClock(Now), logger);

        var credentials = await store.EnsureCredentialsAsync();

        Assert.Equal("0011223344556677", credentials.Openudid);
        Assert.Equal("11111111-2222-3333-4444-555555555555", credentials.Clientudid);
        Assert.Equal(1, client.RegisterCalls);
        Assert.Equal(1, client.TokenCalls);
        Assert.Contains("doubao.credentials.cache_incompatible", logger.LifecycleEvents);
        Assert.DoesNotContain(stale.Openudid, logger.Joined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(stale.Clientudid, logger.Joined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(stale.Token, logger.Joined, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(credentials, await new PlainDoubaoCredentialCache(paths).LoadAsync());
    }

    [Theory]
    [InlineData("device_id", "")]
    [InlineData("install_id", "")]
    [InlineData("cdid", "")]
    [InlineData("openudid", "001122334455667")]
    [InlineData("openudid", "001122334455667A")]
    [InlineData("clientudid", "11111111222233334444555555555555")]
    [InlineData("clientudid", "11111111-2222-3333-4444-55555555555A")]
    public async Task IncompatibleCacheProfileRegistersWithoutLoggingCachedValues(string field, string value)
    {
        using var workspace = TestWorkspace.Create();
        var paths = WindowsUserDataPaths.Create(workspace.ExecutableDirectory, workspace.UserDataRoot);
        var cached = Fixtures.Credentials(token: Fixtures.JwtExpiringAt(Now.AddHours(2)));
        cached = field switch
        {
            "device_id" => cached with { DeviceId = value },
            "install_id" => cached with { InstallId = value },
            "cdid" => cached with { Cdid = value },
            "openudid" => cached with { Openudid = value },
            "clientudid" => cached with { Clientudid = value },
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        };
        await new PlainDoubaoCredentialCache(paths).SaveAsync(cached);
        var client = new FakeDoubaoCredentialClient();
        var logger = new RecordingDiagnosticLog();
        var store = new DoubaoCredentialStore(paths, client, new FixedClock(Now), logger);

        await store.EnsureCredentialsAsync();

        Assert.Equal(1, client.RegisterCalls);
        Assert.Equal(1, client.TokenCalls);
        Assert.Contains("doubao.credentials.cache_incompatible", logger.LifecycleEvents);
        foreach (var cachedValue in new[]
                 {
                     cached.Token,
                     cached.DeviceId,
                     cached.InstallId,
                     cached.Cdid,
                     cached.Openudid,
                     cached.Clientudid
                 }.Where(cachedValue => !string.IsNullOrEmpty(cachedValue)))
        {
            Assert.DoesNotContain(cachedValue, logger.Joined, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task ExpiredTokenRefreshesAndPersistsExistingDeviceCredentials()
    {
        using var workspace = TestWorkspace.Create();
        var paths = WindowsUserDataPaths.Create(workspace.ExecutableDirectory, workspace.UserDataRoot);
        var expired = Fixtures.Credentials(token: Fixtures.JwtExpiringAt(Now.AddMinutes(-1)));
        await new PlainDoubaoCredentialCache(paths).SaveAsync(expired);
        var client = new FakeDoubaoCredentialClient { Token = "fresh-token" };
        var store = new DoubaoCredentialStore(paths, client, new FixedClock(Now), new RecordingDiagnosticLog());

        var credentials = await store.EnsureCredentialsAsync();

        Assert.Equal(expired.DeviceId, credentials.DeviceId);
        Assert.Equal("fresh-token", credentials.Token);
        Assert.Equal(0, client.RegisterCalls);
        Assert.Equal(1, client.TokenCalls);
        var persisted = await new PlainDoubaoCredentialCache(paths).LoadAsync();
        Assert.Equal("fresh-token", persisted?.Token);
    }

    [Fact]
    public async Task MalformedCacheIsIgnoredAndReplaced()
    {
        using var workspace = TestWorkspace.Create();
        var paths = WindowsUserDataPaths.Create(workspace.ExecutableDirectory, workspace.UserDataRoot);
        Directory.CreateDirectory(paths.DoubaoCredentialsDirectory);
        File.WriteAllText(paths.DoubaoCredentialsFilePath, "{ nope");
        var client = new FakeDoubaoCredentialClient();
        var logger = new RecordingDiagnosticLog();
        var store = new DoubaoCredentialStore(paths, client, new FixedClock(Now), logger);

        var credentials = await store.EnsureCredentialsAsync();

        Assert.Equal("device-1", credentials.DeviceId);
        Assert.Equal(1, client.RegisterCalls);
        Assert.Contains(("Doubao", "credentials_cache_malformed", "InvalidOperationException"), logger.Errors);
    }

    [Fact]
    public async Task RegistrationFailureDoesNotFetchToken()
    {
        using var workspace = TestWorkspace.Create();
        var client = new FakeDoubaoCredentialClient { RegisterError = new InvalidOperationException("register failed with secret") };
        var logger = new RecordingDiagnosticLog();
        var store = new DoubaoCredentialStore(
            WindowsUserDataPaths.Create(workspace.ExecutableDirectory, workspace.UserDataRoot),
            client,
            new FixedClock(Now),
            logger);

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.EnsureCredentialsAsync());

        Assert.Equal(1, client.RegisterCalls);
        Assert.Equal(0, client.TokenCalls);
        Assert.Contains(("Doubao", "credentials_registration_failed", "InvalidOperationException"), logger.Errors);
        Assert.DoesNotContain("secret", logger.Joined);
    }

    [Fact]
    public async Task TokenFailureIsLoggedWithoutPersistingPartialCredentials()
    {
        using var workspace = TestWorkspace.Create();
        var paths = WindowsUserDataPaths.Create(workspace.ExecutableDirectory, workspace.UserDataRoot);
        var client = new FakeDoubaoCredentialClient { TokenError = new InvalidOperationException("token secret") };
        var logger = new RecordingDiagnosticLog();
        var store = new DoubaoCredentialStore(paths, client, new FixedClock(Now), logger);

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.EnsureCredentialsAsync());

        Assert.Equal(1, client.RegisterCalls);
        Assert.Equal(1, client.TokenCalls);
        Assert.False(File.Exists(paths.DoubaoCredentialsFilePath));
        Assert.Contains(("Doubao", "credentials_token_failed", "InvalidOperationException"), logger.Errors);
        Assert.DoesNotContain("token secret", logger.Joined);
    }

    [Fact]
    public void JwtExpiryDetectionUsesExpiryAndSafetyMargin()
    {
        Assert.True(DoubaoJwtExpiry.IsExpired(Fixtures.JwtExpiringAt(Now.AddSeconds(59)), Now));
        Assert.False(DoubaoJwtExpiry.IsExpired(Fixtures.JwtExpiringAt(Now.AddSeconds(61)), Now));
        Assert.False(DoubaoJwtExpiry.IsExpired("not.a.jwt", Now));
    }

    [Fact]
    public async Task HttpCredentialClientGeneratesMacosParityAnonymousDeviceIdentifiers()
    {
        var handler = new CapturingHandler("""
            {"device_id_str":"device-1","install_id_str":"install-1"}
            """);
        using var httpClient = new HttpClient(handler);
        using var client = new HttpDoubaoCredentialClient(httpClient);

        var device = await client.RegisterDeviceAsync();

        using var body = JsonDocument.Parse(handler.RequestBody);
        var header = body.RootElement.GetProperty("header");
        var openudid = header.GetProperty("openudid").GetString() ?? "";
        var clientudid = header.GetProperty("clientudid").GetString() ?? "";
        Assert.Equal(device.Openudid, openudid);
        Assert.Equal(device.Clientudid, clientudid);
        Assert.Matches("^[0-9a-f]{16}$", openudid);
        Assert.Matches("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", clientudid);
    }

    private sealed class FakeDoubaoCredentialClient : IDoubaoCredentialClient
    {
        public int RegisterCalls { get; private set; }

        public int TokenCalls { get; private set; }

        public string Token { get; init; } = "token-valid";

        public Exception? RegisterError { get; init; }

        public Exception? TokenError { get; init; }

        public Task<RegisteredDoubaoDevice> RegisterDeviceAsync(CancellationToken cancellationToken = default)
        {
            RegisterCalls++;
            return RegisterError is null
                ? Task.FromResult(new RegisteredDoubaoDevice(
                    "device-1",
                    "install-1",
                    "cdid-1",
                    "0011223344556677",
                    "11111111-2222-3333-4444-555555555555"))
                : Task.FromException<RegisteredDoubaoDevice>(RegisterError);
        }

        public Task<string> FetchTokenAsync(RegisteredDoubaoDevice device, CancellationToken cancellationToken = default)
        {
            TokenCalls++;
            return TokenError is null
                ? Task.FromResult(Token)
                : Task.FromException<string>(TokenError);
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset Now => now;
    }

    private sealed class CapturingHandler(string responseBody) : HttpMessageHandler
    {
        public string RequestBody { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBody = request.Content is null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody)
            };
        }
    }
}
