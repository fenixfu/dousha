namespace Dousha.Windows.Core;

public static class DoubaoWebSocketHandshakeHeaders
{
    public static IReadOnlyDictionary<string, string> Create()
    {
        return new Dictionary<string, string>
        {
            ["User-Agent"] = DoubaoProtocol.UserAgent,
            ["proto-version"] = "v2",
            ["x-custom-keepalive"] = "true"
        };
    }
}
