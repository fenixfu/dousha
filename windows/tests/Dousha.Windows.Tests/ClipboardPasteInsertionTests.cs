using Dousha.Windows.Core;
using Xunit;

namespace Dousha.Windows.Tests;

public sealed class ClipboardPasteInsertionTests
{
    [Fact]
    public async Task NonEmptyTranscriptWritesClipboardThenPostsPaste()
    {
        var clipboard = new FakeClipboardTextWriter();
        var paste = new FakePasteCommandSender();
        var insertion = new ClipboardPasteInsertion(clipboard, paste);

        await insertion.InsertAsync("你好，Windows");

        Assert.Equal("你好，Windows", clipboard.Text);
        Assert.Equal(["clipboard:你好，Windows", "paste"], clipboard.Events.Concat(paste.Events));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyTranscriptDoesNotWriteClipboardOrPaste(string transcript)
    {
        var clipboard = new FakeClipboardTextWriter();
        var paste = new FakePasteCommandSender();
        var insertion = new ClipboardPasteInsertion(clipboard, paste);

        await insertion.InsertAsync(transcript);

        Assert.Null(clipboard.Text);
        Assert.Empty(clipboard.Events);
        Assert.Empty(paste.Events);
    }

    [Fact]
    public async Task PreviousClipboardContentIsNotRestored()
    {
        var clipboard = new FakeClipboardTextWriter { Text = "previous clipboard" };
        var paste = new FakePasteCommandSender();
        var insertion = new ClipboardPasteInsertion(clipboard, paste);

        await insertion.InsertAsync("final transcript");

        Assert.Equal("final transcript", clipboard.Text);
        Assert.DoesNotContain("clipboard:previous clipboard", clipboard.Events);
        Assert.DoesNotContain("restore", clipboard.Events);
    }

    [Fact]
    public async Task PasteIsNotPostedIfClipboardWriteFails()
    {
        var clipboard = new FakeClipboardTextWriter { Error = new InvalidOperationException("clipboard busy") };
        var paste = new FakePasteCommandSender();
        var insertion = new ClipboardPasteInsertion(clipboard, paste);

        await Assert.ThrowsAsync<InvalidOperationException>(() => insertion.InsertAsync("hello"));

        Assert.Empty(paste.Events);
    }

    private sealed class FakeClipboardTextWriter : IClipboardTextWriter
    {
        public List<string> Events { get; } = [];

        public string? Text { get; set; }

        public Exception? Error { get; init; }

        public Task SetTextAsync(string text, CancellationToken cancellationToken = default)
        {
            if (Error is not null)
            {
                return Task.FromException(Error);
            }

            Text = text;
            Events.Add($"clipboard:{text}");
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasteCommandSender : IPasteCommandSender
    {
        public List<string> Events { get; } = [];

        public Task SendPasteAsync(CancellationToken cancellationToken = default)
        {
            Events.Add("paste");
            return Task.CompletedTask;
        }
    }
}
