using System.Net.WebSockets;

namespace Dousha.Windows.Core;

public sealed class WebSocketDoubaoTransportClientFactory : IDoubaoTransportClientFactory
{
    private readonly Uri _endpoint;
    private readonly IDiagnosticLog? _diagnosticLog;

    public WebSocketDoubaoTransportClientFactory()
        : this(new Uri(DoubaoProtocol.WebSocketEndpoint))
    {
    }

    public WebSocketDoubaoTransportClientFactory(IDiagnosticLog? diagnosticLog)
        : this(new Uri(DoubaoProtocol.WebSocketEndpoint), diagnosticLog)
    {
    }

    public WebSocketDoubaoTransportClientFactory(Uri endpoint, IDiagnosticLog? diagnosticLog = null)
    {
        _endpoint = endpoint;
        _diagnosticLog = diagnosticLog;
    }

    public async Task<IDoubaoTransportClient> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var socket = new ClientWebSocket();
        foreach (var header in DoubaoWebSocketHandshakeHeaders.Create())
        {
            socket.Options.SetRequestHeader(header.Key, header.Value);
        }

        _diagnosticLog?.Lifecycle("doubao.websocket.connecting");
        try
        {
            await socket.ConnectAsync(_endpoint, cancellationToken);
            _diagnosticLog?.Lifecycle($"doubao.websocket.connected state={socket.State}");
            return new WebSocketDoubaoTransportClient(socket, _diagnosticLog);
        }
        catch (Exception exception)
        {
            _diagnosticLog?.Error(DiagnosticArea.Doubao, "websocket_connect_failed", exception);
            socket.Dispose();
            throw;
        }
    }
}
