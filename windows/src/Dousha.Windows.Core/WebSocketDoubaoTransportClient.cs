using System.Net.WebSockets;

namespace Dousha.Windows.Core;

public sealed class WebSocketDoubaoTransportClient : IDoubaoTransportClient
{
    private readonly ClientWebSocket _socket;

    public WebSocketDoubaoTransportClient(ClientWebSocket socket)
    {
        _socket = socket;
    }

    public async Task SendAsync(byte[] message, CancellationToken cancellationToken = default)
    {
        await _socket.SendAsync(message, WebSocketMessageType.Binary, endOfMessage: true, cancellationToken);
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
                throw new InvalidOperationException("Doubao WebSocket closed before a complete response was received.");
            }

            stream.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
            {
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
