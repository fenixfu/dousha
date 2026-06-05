namespace Dousha.Windows.Core;

public interface IDoubaoCredentialClient
{
    Task<RegisteredDoubaoDevice> RegisterDeviceAsync(CancellationToken cancellationToken = default);

    Task<string> FetchTokenAsync(RegisteredDoubaoDevice device, CancellationToken cancellationToken = default);
}
