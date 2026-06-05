namespace Dousha.Windows.Core;

public sealed record DoubaoHttpRequest(
    string Method,
    string Url,
    IReadOnlyDictionary<string, string> Headers,
    string Body);
