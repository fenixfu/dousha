namespace Dousha.Windows.Core;

public interface IClipboardTextWriter
{
    Task SetTextAsync(string text, CancellationToken cancellationToken = default);
}
