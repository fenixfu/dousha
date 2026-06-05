using System.Net.WebSockets;

namespace Dousha.Windows.Core;

public sealed class WebSocketDoubaoTransportClientFactory : IDoubaoTransportClientFactory
{
    private readonly IDiagnosticLog? _diagnosticLog;

    public WebSocketDoubaoTransportClientFactory()
        : this(null)
    {
    }

    public WebSocketDoubaoTransportClientFactory(IDiagnosticLog? diagnosticLog)
    {
        _diagnosticLog = diagnosticLog;
    }

    public async Task<IDoubaoTransportClient> ConnectAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        var socket = CreateConfiguredSocket();
        foreach (var header in DoubaoWebSocketHandshakeHeaders.Create())
        {
            socket.Options.SetRequestHeader(header.Key, header.Value);
        }

        var endpoint = DoubaoProtocol.BuildWebSocketUri(deviceId);
        _diagnosticLog?.Lifecycle("doubao.websocket.connecting");
        try
        {
            await socket.ConnectAsync(endpoint, cancellationToken);
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

    public static ClientWebSocket CreateConfiguredSocket()
    {
        var socket = new ClientWebSocket();
        socket.Options.KeepAliveInterval = DoubaoProtocol.WebSocketKeepaliveInterval;
        return socket;
    }
}
