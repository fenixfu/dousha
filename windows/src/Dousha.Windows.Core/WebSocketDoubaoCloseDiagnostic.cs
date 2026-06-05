using System.Net.WebSockets;

namespace Dousha.Windows.Core;

public static class WebSocketDoubaoCloseDiagnostic
{
    public static string Format(WebSocketCloseStatus? closeStatus, string? closeStatusDescription)
    {
        var status = closeStatus?.ToString() ?? "None";
        var reasonLength = closeStatusDescription?.Length ?? 0;
        return $"doubao.websocket.closed closeStatus={status} reasonLength={reasonLength}";
    }
}
