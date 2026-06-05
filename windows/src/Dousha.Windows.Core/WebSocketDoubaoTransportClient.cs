using System.Net.WebSockets;

namespace Dousha.Windows.Core;

public sealed class WebSocketDoubaoTransportClient : IDoubaoTransportClient
{
    private readonly ClientWebSocket _socket;
    private readonly IDiagnosticLog? _diagnosticLog;

    public WebSocketDoubaoTransportClient(ClientWebSocket socket, IDiagnosticLog? diagnosticLog = null)
    {
        _socket = socket;
        _diagnosticLog = diagnosticLog;
    }

    public async Task SendAsync(byte[] message, CancellationToken cancellationToken = default)
    {
        try
        {
            await _socket.SendAsync(message, WebSocketMessageType.Binary, endOfMessage: true, cancellationToken);
            _diagnosticLog?.Lifecycle($"doubao.websocket.sent bytes={message.Length} state={_socket.State}");
        }
        catch (Exception exception)
        {
            _diagnosticLog?.Error(DiagnosticArea.Doubao, "websocket_send_failed", exception);
            throw;
        }
    }

    public async Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        var buffer = new byte[8192];
        using var stream = new MemoryStream();
        while (true)
        {
            var result = await _socket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                _diagnosticLog?.Lifecycle(WebSocketDoubaoCloseDiagnostic.Format(
                    _socket.CloseStatus,
                    _socket.CloseStatusDescription));
                throw new InvalidOperationException("Doubao WebSocket closed before a complete response was received.");
            }

            stream.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
            {
                _diagnosticLog?.Lifecycle($"doubao.websocket.received bytes={stream.Length}");
                return stream.ToArray();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Dousha shutdown", CancellationToken.None);
        }

        _socket.Dispose();
    }
}
