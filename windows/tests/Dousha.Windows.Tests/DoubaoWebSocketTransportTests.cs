using Dousha.Windows.Core;
using System.Net.WebSockets;
using Xunit;

namespace Dousha.Windows.Tests;

public sealed class DoubaoWebSocketTransportTests
{
    [Fact]
    public void HandshakeHeadersMirrorDoubaoMacOsPriorArt()
    {
        var headers = DoubaoWebSocketHandshakeHeaders.Create();

        Assert.Equal(DoubaoProtocol.UserAgent, headers["User-Agent"]);
        Assert.Equal("v2", headers["proto-version"]);
        Assert.Equal("true", headers["x-custom-keepalive"]);
    }

    [Fact]
    public void CloseDiagnosticIncludesStatusAndReasonLengthWithoutReasonText()
    {
        var diagnostic = WebSocketDoubaoCloseDiagnostic.Format(
            WebSocketCloseStatus.PolicyViolation,
            "token-secret rejected");

        Assert.Contains("closeStatus=PolicyViolation", diagnostic);
        Assert.Contains("reasonLength=21", diagnostic);
        Assert.DoesNotContain("token-secret", diagnostic);
        Assert.DoesNotContain("rejected", diagnostic);
    }
}
