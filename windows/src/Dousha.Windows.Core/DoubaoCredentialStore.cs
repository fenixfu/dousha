namespace Dousha.Windows.Core;

public sealed class DoubaoCredentialStore
{
    private readonly PlainDoubaoCredentialCache _cache;
    private readonly IDoubaoCredentialClient _client;
    private readonly IClock _clock;
    private readonly IDiagnosticLog _diagnosticLog;

    public DoubaoCredentialStore(
        WindowsUserDataPaths paths,
        IDoubaoCredentialClient client,
        IClock clock,
        IDiagnosticLog diagnosticLog)
    {
        _cache = new PlainDoubaoCredentialCache(paths);
        _client = client;
        _clock = clock;
        _diagnosticLog = diagnosticLog;
    }

    public async Task<DoubaoDeviceCredentials> EnsureCredentialsAsync(CancellationToken cancellationToken = default)
    {
        var cached = await LoadCachedCredentialsAsync(cancellationToken);
        if (cached is not null && !IsCompatibleCacheProfile(cached))
        {
            _diagnosticLog.Lifecycle("doubao.credentials.cache_incompatible");
            cached = null;
        }

        if (cached is not null)
        {
            if (!string.IsNullOrWhiteSpace(cached.Token) && !DoubaoJwtExpiry.IsExpired(cached.Token, _clock.Now))
            {
                _diagnosticLog.Lifecycle("doubao.credentials.cache_hit");
                return cached;
            }

            _diagnosticLog.Lifecycle("doubao.credentials.token_refresh");
            var refreshedToken = await FetchTokenWithLoggingAsync(
                new RegisteredDoubaoDevice(cached.DeviceId, cached.InstallId, cached.Cdid, cached.Openudid, cached.Clientudid),
                cancellationToken);
            var refreshed = cached with { Token = refreshedToken };
            await _cache.SaveAsync(refreshed, cancellationToken);
            return refreshed;
        }

        _diagnosticLog.Lifecycle("doubao.credentials.cache_miss");
        RegisteredDoubaoDevice registered;
        try
        {
            registered = await _client.RegisterDeviceAsync(cancellationToken);
            _diagnosticLog.Lifecycle("doubao.credentials.registered");
        }
        catch (Exception exception)
        {
            _diagnosticLog.Error(DiagnosticArea.Doubao, "credentials_registration_failed", exception);
            throw;
        }

        var token = await FetchTokenWithLoggingAsync(registered, cancellationToken);
        var credentials = new DoubaoDeviceCredentials(
            registered.DeviceId,
            registered.InstallId,
            registered.Cdid,
            registered.Openudid,
            registered.Clientudid,
            token);
        await _cache.SaveAsync(credentials, cancellationToken);
        _diagnosticLog.Lifecycle("doubao.credentials.saved");
        return credentials;
    }

    private static bool IsCompatibleCacheProfile(DoubaoDeviceCredentials credentials)
    {
        return !string.IsNullOrWhiteSpace(credentials.DeviceId)
            && !string.IsNullOrWhiteSpace(credentials.InstallId)
            && !string.IsNullOrWhiteSpace(credentials.Cdid)
            && IsLowercaseHex(credentials.Openudid, 16)
            && IsCanonicalLowercaseUuid(credentials.Clientudid);
    }

    private static bool IsLowercaseHex(string value, int length)
    {
        return value is not null
            && value.Length == length
            && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
    }

    private static bool IsCanonicalLowercaseUuid(string value)
    {
        return Guid.TryParseExact(value, "D", out var parsed)
            && string.Equals(value, parsed.ToString("D"), StringComparison.Ordinal);
    }

    private async Task<DoubaoDeviceCredentials?> LoadCachedCredentialsAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _cache.LoadAsync(cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            _diagnosticLog.Error(DiagnosticArea.Doubao, "credentials_cache_malformed", exception);
            return null;
        }
    }

    private async Task<string> FetchTokenWithLoggingAsync(RegisteredDoubaoDevice registered, CancellationToken cancellationToken)
    {
        try
        {
            var token = await _client.FetchTokenAsync(registered, cancellationToken);
            _diagnosticLog.Lifecycle("doubao.credentials.token_fetched");
            return token;
        }
        catch (Exception exception)
        {
            _diagnosticLog.Error(DiagnosticArea.Doubao, "credentials_token_failed", exception);
            throw;
        }
    }
}
