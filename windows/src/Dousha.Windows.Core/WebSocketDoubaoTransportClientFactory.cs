using System.Net.WebSockets;

namespace Dousha.Windows.Core;

public sealed class WebSocketDoubaoTransportClientFactory : IDoubaoTransportClientFactory
{
    private readonly Uri _endpoint;

    public WebSocketDoubaoTransportClientFactory()
        : this(new Uri(DoubaoProtocol.WebSocketEndpoint))
    {
    }

    public WebSocketDoubaoTransportClientFactory(Uri endpoint)
    {
        _endpoint = endpoint;
    }

    public async Task<IDoubaoTransportClient> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var socket = new ClientWebSocket();
        socket.Options.SetRequestHeader("User-Agent", DoubaoProtocol.UserAgent);
        await socket.ConnectAsync(_endpoint, cancellationToken);
        return new WebSocketDoubaoTransportClient(socket);
    }
}
