using Dousha.Windows.Core;
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
                ? Task.FromResult(new RegisteredDoubaoDevice("device-1", "install-1", "cdid-1", "open-1", "client-1"))
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
}
