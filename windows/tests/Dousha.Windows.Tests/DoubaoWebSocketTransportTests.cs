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
    public void WebSocketUriIncludesAidAndDeviceIdAfterCredentialsAreKnown()
    {
        var uri = DoubaoProtocol.BuildWebSocketUri("device-1");

        Assert.Equal(
            "wss://frontier-audio-ime-ws.doubao.com/ocean/api/v1/ws?aid=401734&device_id=device-1",
            uri.AbsoluteUri);
    }

    [Fact]
    public void WebSocketUriEscapesDeviceIdQueryValue()
    {
        var uri = DoubaoProtocol.BuildWebSocketUri("device with spaces");

        Assert.Equal(
            "wss://frontier-audio-ime-ws.doubao.com/ocean/api/v1/ws?aid=401734&device_id=device%20with%20spaces",
            uri.AbsoluteUri);
    }

    [Fact]
    public void WebSocketKeepaliveIntervalMatchesMacosPingCadence()
    {
        using var socket = WebSocketDoubaoTransportClientFactory.CreateConfiguredSocket();

        Assert.Equal(TimeSpan.FromSeconds(3), socket.Options.KeepAliveInterval);
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
