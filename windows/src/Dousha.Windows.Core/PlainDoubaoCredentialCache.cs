using System.Text.Json;

namespace Dousha.Windows.Core;

public sealed class PlainDoubaoCredentialCache
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly WindowsUserDataPaths _paths;

    public PlainDoubaoCredentialCache(WindowsUserDataPaths paths)
    {
        _paths = paths;
    }

    public async Task<DoubaoDeviceCredentials?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_paths.DoubaoCredentialsFilePath))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(_paths.DoubaoCredentialsFilePath);
            return await JsonSerializer.DeserializeAsync<DoubaoDeviceCredentials>(stream, JsonOptions, cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Malformed Doubao credential cache.", exception);
        }
    }

    public async Task SaveAsync(DoubaoDeviceCredentials credentials, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_paths.DoubaoCredentialsDirectory);
        await using var stream = File.Create(_paths.DoubaoCredentialsFilePath);
        await JsonSerializer.SerializeAsync(stream, credentials, JsonOptions, cancellationToken);
    }
}
