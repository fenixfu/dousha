using Dousha.Windows.Core;
using System.Runtime.InteropServices;

namespace Dousha.Windows.App;

public sealed class WindowsPasteCommandSender : IPasteCommandSender
{
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_V = 0x56;
    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private readonly IWindowsInputApi _inputApi;

    internal static int InputSize => Marshal.SizeOf<Input>();

    public WindowsPasteCommandSender()
        : this(NativeWindowsInputApi.Instance)
    {
    }

    internal WindowsPasteCommandSender(IWindowsInputApi inputApi)
    {
        _inputApi = inputApi;
    }

    public Task SendPasteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var inputs = new Input[]
        {
            KeyboardInput(VK_CONTROL, keyUp: false),
            KeyboardInput(VK_V, keyUp: false),
            KeyboardInput(VK_V, keyUp: true),
            KeyboardInput(VK_CONTROL, keyUp: true)
        };

        var result = _inputApi.Send(inputs, InputSize);
        if (result.Sent != inputs.Length)
        {
            throw new InvalidOperationException(
                $"Failed to send Ctrl+V paste command. sent={result.Sent} expected={inputs.Length} win32_error={result.ErrorCode}.");
        }

        return Task.CompletedTask;
    }

    private static Input KeyboardInput(ushort keyCode, bool keyUp)
    {
        return new Input
        {
            Type = INPUT_KEYBOARD,
            Data = new InputUnion
            {
                Keyboard = new KeyboardInputData
                {
                    VirtualKey = keyCode,
                    Flags = keyUp ? KEYEVENTF_KEYUP : 0
                }
            }
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Input
    {
        public uint Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct InputUnion
    {
        [FieldOffset(0)]
        public KeyboardInputData Keyboard;

        [FieldOffset(0)]
        public MouseInputData Mouse;

        [FieldOffset(0)]
        public HardwareInputData Hardware;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KeyboardInputData
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public nint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MouseInputData
    {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public nint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HardwareInputData
    {
        public uint Message;
        public ushort ParameterLow;
        public ushort ParameterHigh;
    }
}

internal interface IWindowsInputApi
{
    WindowsInputSendResult Send(WindowsPasteCommandSender.Input[] inputs, int inputSize);
}

internal readonly record struct WindowsInputSendResult(uint Sent, int ErrorCode);

internal sealed class NativeWindowsInputApi : IWindowsInputApi
{
    public static NativeWindowsInputApi Instance { get; } = new();

    private NativeWindowsInputApi()
    {
    }

    public WindowsInputSendResult Send(WindowsPasteCommandSender.Input[] inputs, int inputSize)
    {
        var sent = SendInput((uint)inputs.Length, inputs, inputSize);
        var errorCode = sent == inputs.Length ? 0 : Marshal.GetLastWin32Error();
        return new WindowsInputSendResult(sent, errorCode);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(
        uint inputCount,
        WindowsPasteCommandSender.Input[] inputs,
        int inputSize);
}
