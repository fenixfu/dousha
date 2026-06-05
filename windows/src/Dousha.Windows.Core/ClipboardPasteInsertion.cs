namespace Dousha.Windows.Core;

public sealed class ClipboardPasteInsertion : ITextInsertion
{
    private readonly IClipboardTextWriter _clipboard;
    private readonly IPasteCommandSender _pasteCommand;

    public ClipboardPasteInsertion(IClipboardTextWriter clipboard, IPasteCommandSender pasteCommand)
    {
        _clipboard = clipboard;
        _pasteCommand = pasteCommand;
    }

    public async Task InsertAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        await _clipboard.SetTextAsync(text, cancellationToken);
        await _pasteCommand.SendPasteAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
