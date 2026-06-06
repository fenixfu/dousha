using Dousha.Windows.App;
using Dousha.Windows.Core;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace Dousha.Windows.Tests;

public sealed class WindowsPasteCommandSenderTests
{
    [Fact]
    public void InputUsesNativeWin32AbiSize()
    {
        var expected = IntPtr.Size switch
        {
            4 => 28,
            8 => 40,
            _ => throw new PlatformNotSupportedException()
        };

        Assert.Equal(expected, WindowsPasteCommandSender.InputSize);
    }

    [Fact]
    public void NativeSendInputPreservesLastErrorForImmediateRetrieval()
    {
        var method = typeof(NativeWindowsInputApi).GetMethod(
            "SendInput",
            BindingFlags.NonPublic | BindingFlags.Static);
        var import = method?.GetCustomAttribute<DllImportAttribute>();

        Assert.NotNull(import);
        Assert.True(import.SetLastError);
    }

    [Fact]
    public async Task SendsExactControlVPasteSequence()
    {
        var inputApi = new RecordingWindowsInputApi
        {
            Result = new WindowsInputSendResult(Sent: 4, ErrorCode: 0)
        };
        var sender = new WindowsPasteCommandSender(inputApi);

        await sender.SendPasteAsync();

        Assert.Equal(WindowsPasteCommandSender.InputSize, inputApi.InputSize);
        Assert.Collection(
            inputApi.Inputs,
            input => AssertKeyboardInput(input, virtualKey: 0x11, flags: 0),
            input => AssertKeyboardInput(input, virtualKey: 0x56, flags: 0),
            input => AssertKeyboardInput(input, virtualKey: 0x56, flags: 0x0002),
            input => AssertKeyboardInput(input, virtualKey: 0x11, flags: 0x0002));
    }

    [Fact]
    public async Task PartialNativeSendReportsSafeCountsAndWin32Error()
    {
        const string transcript = "private dictated transcript";
        var inputApi = new RecordingWindowsInputApi
        {
            Result = new WindowsInputSendResult(Sent: 2, ErrorCode: 5)
        };
        var sender = new WindowsPasteCommandSender(inputApi);
        var clipboard = new RecordingClipboardTextWriter();
        var insertion = new ClipboardPasteInsertion(clipboard, sender);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => insertion.InsertAsync(transcript));

        Assert.Equal(transcript, clipboard.Text);
        Assert.Equal(
            "Failed to send Ctrl+V paste command. sent=2 expected=4 win32_error=5.",
            exception.Message);
        Assert.DoesNotContain(transcript, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertKeyboardInput(
        WindowsPasteCommandSender.Input input,
        ushort virtualKey,
        uint flags)
    {
        Assert.Equal(1u, input.Type);
        Assert.Equal(virtualKey, input.Data.Keyboard.VirtualKey);
        Assert.Equal(flags, input.Data.Keyboard.Flags);
    }

    private sealed class RecordingWindowsInputApi : IWindowsInputApi
    {
        public WindowsInputSendResult Result { get; init; }

        public WindowsPasteCommandSender.Input[] Inputs { get; private set; } = [];

        public int InputSize { get; private set; }

        public WindowsInputSendResult Send(WindowsPasteCommandSender.Input[] inputs, int inputSize)
        {
            Inputs = inputs.ToArray();
            InputSize = inputSize;
            return Result;
        }
    }

    private sealed class RecordingClipboardTextWriter : IClipboardTextWriter
    {
        public string? Text { get; private set; }

        public Task SetTextAsync(string text, CancellationToken cancellationToken = default)
        {
            Text = text;
            return Task.CompletedTask;
        }
    }
}
