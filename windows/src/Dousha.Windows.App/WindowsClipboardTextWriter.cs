using Dousha.Windows.Core;
using System.Windows.Forms;

namespace Dousha.Windows.App;

public sealed class WindowsClipboardTextWriter : IClipboardTextWriter
{
    public Task SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Clipboard.SetText(text, TextDataFormat.UnicodeText);
        return Task.CompletedTask;
    }
}
