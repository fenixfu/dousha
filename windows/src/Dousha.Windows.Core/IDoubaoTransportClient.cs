namespace Dousha.Windows.Core;

public interface IDoubaoTransportClient : IAsyncDisposable
{
    Task SendAsync(byte[] message, CancellationToken cancellationToken = default);

    Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default);
}
