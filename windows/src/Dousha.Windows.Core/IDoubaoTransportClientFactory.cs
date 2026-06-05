namespace Dousha.Windows.Core;

public interface IDoubaoTransportClientFactory
{
    Task<IDoubaoTransportClient> ConnectAsync(string deviceId, CancellationToken cancellationToken = default);
}
