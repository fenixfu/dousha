namespace Dousha.Windows.Core;

public interface IDoubaoTransportClientFactory
{
    Task<IDoubaoTransportClient> ConnectAsync(CancellationToken cancellationToken = default);
}
