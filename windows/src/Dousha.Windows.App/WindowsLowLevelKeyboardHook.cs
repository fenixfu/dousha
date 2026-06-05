using Dousha.Windows.Core;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Dousha.Windows.App;

public sealed class WindowsLowLevelKeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;
    private const int VK_LCONTROL = 0xA2;

    private readonly DoubleTapHoldTrigger _trigger;
    private readonly Action<TriggerCommand> _commandSink;
    private readonly LowLevelKeyboardProc _callback;
    private IntPtr _hookHandle;

    public WindowsLowLevelKeyboardHook(TriggerSettings settings, Action<TriggerCommand> commandSink)
    {
        _trigger = new DoubleTapHoldTrigger(settings);
        _commandSink = commandSink;
        _callback = HookCallback;
    }

    public void Start()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            return;
        }

        _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _callback, GetModuleHandle(null), 0);
        if (_hookHandle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to install low-level keyboard hook.");
        }
    }

    public void Dispose()
    {
        if (_hookHandle == IntPtr.Zero)
        {
            return;
        }

        UnhookWindowsHookEx(_hookHandle);
        _hookHandle = IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && TryMapEvent(wParam, lParam, out var triggerEvent))
        {
            var result = _trigger.Handle(triggerEvent);
            foreach (var command in result.Commands)
            {
                _commandSink(command);
            }

            if (result.ShouldSuppressKey)
            {
                return new IntPtr(1);
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private static bool TryMapEvent(IntPtr wParam, IntPtr lParam, out TriggerKeyEvent triggerEvent)
    {
        var message = wParam.ToInt32();
        var data = Marshal.PtrToStructure<Kbdllhookstruct>(lParam);
        var timestamp = DateTimeOffset.Now;
        var isKeyDown = message is WM_KEYDOWN or WM_SYSKEYDOWN;
        var isKeyUp = message is WM_KEYUP or WM_SYSKEYUP;

        if (data.VkCode == VK_LCONTROL)
        {
            if (isKeyDown)
            {
                triggerEvent = TriggerKeyEvent.KeyDown(TriggerKey.LeftControl, timestamp);
                return true;
            }

            if (isKeyUp)
            {
                triggerEvent = TriggerKeyEvent.KeyUp(TriggerKey.LeftControl, timestamp);
                return true;
            }
        }

        if (isKeyDown)
        {
            triggerEvent = TriggerKeyEvent.OtherKeyDown(timestamp);
            return true;
        }

        triggerEvent = TriggerKeyEvent.OtherKeyDown(timestamp);
        return false;
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct Kbdllhookstruct
    {
        public readonly int VkCode;
        public readonly int ScanCode;
        public readonly int Flags;
        public readonly int Time;
        public readonly IntPtr DwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
