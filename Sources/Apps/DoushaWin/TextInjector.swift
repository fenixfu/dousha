// Clipboard paste insertion for the Windows shell (QUA-209).
//
// Write CF_UNICODETEXT to the clipboard, then send Ctrl+V. The clipboard is
// deliberately not restored: targets may consume paste asynchronously, so an
// early restore can make them paste stale content.
//
// Known limit (UIPI): SendInput into a window running elevated (admin) can
// fail silently when Dousha runs non-elevated. Logged, not retried or elevated.
#if os(Windows)
import WinSDK
import Foundation
import ConcurrencySupport

protocol TextInjectorWindowsAPI: AnyObject {
    func openClipboard(owner: HWND?) -> Bool
    func sleep(milliseconds: DWORD)
    func closeClipboard()
    func emptyClipboard() -> Bool
    func allocateMovableMemory(byteCount: Int) -> HGLOBAL?
    func lockMemory(_ memory: HGLOBAL) -> UnsafeMutableRawPointer?
    func unlockMemory(_ memory: HGLOBAL)
    func setUnicodeClipboardData(_ memory: HGLOBAL) -> Bool
    func freeMemory(_ memory: HGLOBAL)
    func sendInput(_ inputs: inout [INPUT]) -> (sent: UINT, error: DWORD)
    func lastError() -> DWORD
    func foregroundWindowTitle() -> String?
}

/// Sendable because the owner HWND is immutable and remains valid for the
/// process lifetime; it is passed back to Win32 but never dereferenced.
struct TextInjector: @unchecked Sendable {
    private static let openClipboardAttemptCount = 5
    private static let openClipboardRetryDelayMilliseconds = DWORD(10)

    private let owner: HWND?

    init(owner: HWND?) {
        self.owner = owner
    }

    func type(_ text: String) {
        Self.type(
            text,
            owner: owner,
            api: NativeTextInjectorWindowsAPI(),
            xwaylandWindowTitlePrefix: gConfig.xwaylandWindowTitlePrefix,
            xwaylandPasteShortcut: gConfig.xwaylandPasteShortcut,
            log: doushaLog
        )
    }

    static func type(
        _ text: String,
        owner: HWND?,
        api: TextInjectorWindowsAPI,
        xwaylandWindowTitlePrefix: String = "",
        xwaylandPasteShortcut: String = "alt+v",
        log: (String) -> Void
    ) {
        guard !text.isEmpty else { return }
        guard !text.utf16.contains(0) else {
            log("[TextInjector] rejected text containing U+0000")
            return
        }
        guard writeUnicodeTextToClipboard(text, owner: owner, api: api, log: log) else { return }

        let shortcut: PasteShortcut
        if let parsed = PasteShortcut(parsing: xwaylandPasteShortcut) {
            shortcut = parsed
        } else {
            shortcut = .default
            log("[TextInjector] invalid xwaylandPasteShortcut '\(xwaylandPasteShortcut)'; falling back to \(shortcut.description)")
        }

        let title = api.foregroundWindowTitle()
        let (inputs, shortcutName) = xwaylandInputsIfMatching(
            title: title,
            prefix: xwaylandWindowTitlePrefix,
            shortcut: shortcut
        )
        if let title, title.hasPrefix(xwaylandWindowTitlePrefix), !xwaylandWindowTitlePrefix.isEmpty {
            log("[TextInjector] WSL Desktop Paste Target detected; sending \(shortcutName)")
        } else if title == nil {
            log("[TextInjector] foreground window title unreadable; falling back to \(shortcutName)")
        } else {
            log("[TextInjector] foreground window title does not match prefix; sending \(shortcutName)")
        }

        var mutableInputs = inputs
        let result = api.sendInput(&mutableInputs)
        guard Int(result.sent) != mutableInputs.count else {
            log("[TextInjector] dispatched \(shortcutName)")
            return
        }

        log("[TextInjector] \(shortcutName) SendInput sent \(result.sent)/\(mutableInputs.count) events (err=\(result.error))")
        var cleanup = keyUpCleanup(for: mutableInputs, afterSentPrefix: Int(result.sent))
        guard !cleanup.isEmpty else { return }

        let cleanupResult = api.sendInput(&cleanup)
        if Int(cleanupResult.sent) != cleanup.count {
            log("[TextInjector] key-up cleanup sent \(cleanupResult.sent)/\(cleanup.count) events (err=\(cleanupResult.error))")
        }
    }

    private static func writeUnicodeTextToClipboard(
        _ text: String,
        owner: HWND?,
        api: TextInjectorWindowsAPI,
        log: (String) -> Void
    ) -> Bool {
        guard openClipboard(owner: owner, api: api) else {
            log("[TextInjector] OpenClipboard failed (err=\(api.lastError()))")
            return false
        }
        defer { api.closeClipboard() }

        guard api.emptyClipboard() else {
            log("[TextInjector] EmptyClipboard failed (err=\(api.lastError()))")
            return false
        }

        var utf16 = Array(text.utf16)
        utf16.append(0)
        let byteCount = utf16.count * MemoryLayout<WCHAR>.size
        guard let memory = api.allocateMovableMemory(byteCount: byteCount) else {
            log("[TextInjector] GlobalAlloc failed (err=\(api.lastError()))")
            return false
        }
        var ownsMemory = true
        defer {
            if ownsMemory {
                api.freeMemory(memory)
            }
        }

        guard let destination = api.lockMemory(memory) else {
            log("[TextInjector] GlobalLock failed (err=\(api.lastError()))")
            return false
        }
        utf16.withUnsafeBytes { source in
            destination.copyMemory(from: source.baseAddress!, byteCount: source.count)
        }
        api.unlockMemory(memory)

        guard api.setUnicodeClipboardData(memory) else {
            log("[TextInjector] SetClipboardData failed (err=\(api.lastError()))")
            return false
        }
        ownsMemory = false
        return true
    }

    private static func openClipboard(owner: HWND?, api: TextInjectorWindowsAPI) -> Bool {
        for attempt in 0..<openClipboardAttemptCount {
            if api.openClipboard(owner: owner) {
                return true
            }
            if attempt + 1 < openClipboardAttemptCount {
                api.sleep(milliseconds: openClipboardRetryDelayMilliseconds)
            }
        }
        return false
    }

    static func pasteInputs() -> [INPUT] {
        [
            keyboardInput(virtualKey: 0x11, keyUp: false), // VK_CONTROL
            keyboardInput(virtualKey: 0x56, keyUp: false), // V
            keyboardInput(virtualKey: 0x56, keyUp: true),
            keyboardInput(virtualKey: 0x11, keyUp: true),
        ]
    }

    private static func xwaylandInputsIfMatching(
        title: String?,
        prefix: String,
        shortcut: PasteShortcut
    ) -> (inputs: [INPUT], name: String) {
        if let title, title.hasPrefix(prefix), !prefix.isEmpty {
            return (shortcut.inputs(), shortcut.description)
        }
        return (pasteInputs(), "Ctrl+V")
    }

    private static func keyUpCleanup(for inputs: [INPUT], afterSentPrefix sent: Int) -> [INPUT] {
        var held: [WORD] = []
        for input in inputs.prefix(sent) {
            guard input.type == DWORD(INPUT_KEYBOARD) else { continue }
            let vk = input.ki.wVk
            if (input.ki.dwFlags & DWORD(KEYEVENTF_KEYUP)) == 0 {
                held.append(vk)
            } else if let index = held.lastIndex(of: vk) {
                held.remove(at: index)
            }
        }
        return held.reversed().map { keyboardInput(virtualKey: $0, keyUp: true) }
    }

    fileprivate static func keyboardInput(virtualKey: WORD, keyUp: Bool) -> INPUT {
        var input = INPUT()
        input.type = DWORD(INPUT_KEYBOARD)
        input.ki = KEYBDINPUT(
            wVk: virtualKey,
            wScan: 0,
            dwFlags: keyUp ? DWORD(KEYEVENTF_KEYUP) : 0,
            time: 0,
            dwExtraInfo: 0
        )
        return input
    }
}

struct PasteShortcut {
    private let modifiers: [Modifier]
    private let key: String

    private enum Modifier: Hashable {
        case alt, ctrl, shift

        var virtualKey: WORD {
            switch self {
            case .alt: return WORD(VK_MENU)
            case .ctrl: return WORD(VK_CONTROL)
            case .shift: return WORD(VK_SHIFT)
            }
        }

        var description: String {
            switch self {
            case .alt: return "alt"
            case .ctrl: return "ctrl"
            case .shift: return "shift"
            }
        }
    }

    init?(parsing string: String) {
        let parts = string.split(separator: "+", omittingEmptySubsequences: false).map(String.init)
        guard parts.count >= 2 else { return nil }
        let keyPart = parts.last!.lowercased()
        guard keyPart.count == 1,
              let keyChar = keyPart.first,
              let ascii = keyChar.asciiValue,
              (ascii >= 0x61 && ascii <= 0x7A) else { return nil }

        var modifiers: [Modifier] = []
        var seen: Set<Modifier> = []
        for part in parts.dropLast() {
            let modifier: Modifier
            switch part.lowercased() {
            case "alt": modifier = .alt
            case "ctrl": modifier = .ctrl
            case "shift": modifier = .shift
            default: return nil
            }
            guard seen.insert(modifier).inserted else { return nil }
            modifiers.append(modifier)
        }
        self.modifiers = modifiers
        self.key = String(keyChar)
    }

    static let `default` = PasteShortcut(parsing: "alt+v")!

    private var keyCode: WORD {
        WORD(key.uppercased().utf16.first!)
    }

    var description: String {
        modifiers.map { $0.description }.joined(separator: "+") + "+\(key)"
    }

    func inputs() -> [INPUT] {
        var inputs: [INPUT] = []
        for modifier in modifiers {
            inputs.append(TextInjector.keyboardInput(virtualKey: modifier.virtualKey, keyUp: false))
        }
        inputs.append(TextInjector.keyboardInput(virtualKey: keyCode, keyUp: false))
        inputs.append(TextInjector.keyboardInput(virtualKey: keyCode, keyUp: true))
        for modifier in modifiers.reversed() {
            inputs.append(TextInjector.keyboardInput(virtualKey: modifier.virtualKey, keyUp: true))
        }
        return inputs
    }
}

private final class NativeTextInjectorWindowsAPI: TextInjectorWindowsAPI {
    func openClipboard(owner: HWND?) -> Bool {
        OpenClipboard(owner)
    }

    func sleep(milliseconds: DWORD) {
        Sleep(milliseconds)
    }

    func closeClipboard() {
        CloseClipboard()
    }

    func emptyClipboard() -> Bool {
        EmptyClipboard()
    }

    func allocateMovableMemory(byteCount: Int) -> HGLOBAL? {
        GlobalAlloc(UINT(GMEM_MOVEABLE), SIZE_T(byteCount))
    }

    func lockMemory(_ memory: HGLOBAL) -> UnsafeMutableRawPointer? {
        GlobalLock(memory)
    }

    func unlockMemory(_ memory: HGLOBAL) {
        GlobalUnlock(memory)
    }

    func setUnicodeClipboardData(_ memory: HGLOBAL) -> Bool {
        SetClipboardData(UINT(CF_UNICODETEXT), memory) != nil
    }

    func freeMemory(_ memory: HGLOBAL) {
        GlobalFree(memory)
    }

    func sendInput(_ inputs: inout [INPUT]) -> (sent: UINT, error: DWORD) {
        let sent = inputs.withUnsafeMutableBufferPointer { buffer in
            SendInput(UINT(buffer.count), buffer.baseAddress, Int32(MemoryLayout<INPUT>.size))
        }
        let error = Int(sent) == inputs.count ? DWORD(0) : GetLastError()
        return (sent, error)
    }

    func lastError() -> DWORD {
        GetLastError()
    }

    func foregroundWindowTitle() -> String? {
        guard let hwnd = GetForegroundWindow() else { return nil }
        let length = Int(GetWindowTextLengthW(hwnd))
        guard length > 0 else { return "" }
        var buffer: [WCHAR] = Array(repeating: 0, count: length + 1)
        let copied = Int(GetWindowTextW(hwnd, &buffer, Int32(buffer.count)))
        guard copied > 0 else { return nil }
        return String(decoding: buffer.prefix(copied), as: UTF16.self)
    }
}
#endif
