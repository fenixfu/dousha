# PRD: Doubao-first Windows MVP tray dictation app

## Problem Statement

The user wants a Windows Port of Dousha for personal use. The existing app is a macOS menu-bar dictation tool whose identity is "Doubao dictation without installing the Doubao IME": trigger recording, speak, receive a Doubao transcript, and insert the text into the active app. A Windows version that uses a generic cloud ASR first would lose the point of the project.

The current macOS implementation is strongly bound to macOS APIs for menu-bar UI, global event taps, microphone permissions, AVFoundation/AudioToolbox Opus encoding, Cocoa pasteboard insertion, launch-at-login, and bundle testing. The Windows Port therefore needs a Windows-native implementation that preserves the Core Dictation Experience while replacing platform-specific integrations.

## Solution

Build the First Usable Windows Version as a Personal Utility: a portable C#/.NET Windows Tray Dictation App with a WinForms shell, a Doubao Dictation Path, Default Microphone Capture, a Double-Tap-and-Hold Trigger on Left Control, Clipboard Paste Insertion, Minimal Status Feedback, Local Diagnostic Log, Non-Blocking Error Feedback, Normal-Privilege Operation, and optional Startup Shortcut support.

The Windows MVP should prove the full end-to-end Doubao loop from the real portable executable: double-tap and hold Left Control, capture the default microphone, automatically manage Doubao Device Credentials, encode and send audio to Doubao, receive Mandarin-First Dictation output, write the transcript to the clipboard, simulate paste into the active application, and return to idle.

## User Stories

1. As the owner of this Personal Utility, I want a Windows Port of Dousha, so that I can use Doubao dictation on Windows without installing the Doubao IME.
2. As the owner, I want the Windows Port to preserve the Core Dictation Experience, so that the app feels like Dousha rather than an unrelated transcription tool.
3. As the owner, I want the First Usable Windows Version to use Doubao, so that the MVP keeps the product identity of the fork.
4. As the owner, I want the app to run from the Windows notification area, so that it behaves like a lightweight background utility.
5. As the owner, I want a tray menu, so that I can open settings, view status, open logs, manage startup, and quit the app.
6. As the owner, I want a lightweight Settings Window, so that I can configure the few behaviors needed for the MVP.
7. As the owner, I want the app to run at normal user privilege, so that the tray tool does not require administrator rights.
8. As the owner, I want the app to avoid targeting elevated administrator windows, so that it respects Windows security boundaries.
9. As the owner, I want the app to be delivered as a Portable Windows Release, so that I can unzip it and run it without an installer.
10. As the owner, I want settings and credentials to persist outside the portable executable directory, so that I can replace the app files without losing configuration.
11. As the owner, I want optional Startup Shortcut support, so that the portable app can start when I sign in.
12. As the owner, I want no automatic updater, so that the project remains a simple self-used tool.
13. As the owner, I want the default trigger to be Double-Tap-and-Hold Left Control, so that I can use push-to-talk even on keyboards without Right Control.
14. As the owner, I want normal Control shortcuts not to be broken, so that copy, paste, save, and tab switching continue working.
15. As the owner, I want recording to start only on the second held Left Control press, so that accidental single Control presses do not trigger dictation.
16. As the owner, I want releasing the held Control key to stop recording, so that the interaction remains push-to-talk rather than toggle-only.
17. As the owner, I want the trigger to be configurable, so that I can adjust the gesture if the default conflicts with my keyboard habits.
18. As the owner, I want the app to use the default microphone, so that MVP setup stays simple.
19. As the owner, I want the app to convert microphone audio into the format Doubao expects, so that the service receives usable audio.
20. As the owner, I want no microphone picker in the MVP, so that the first version focuses on the core dictation loop.
21. As the owner, I want no saved recording files in the MVP, so that the app does not accumulate audio data unnecessarily.
22. As the owner, I want Doubao Device Credentials to be obtained and refreshed automatically, so that I do not have to manually paste tokens.
23. As the owner, I want the MVP to use a Plain Credential Cache, so that implementation stays simple for a self-used first version.
24. As the owner, I want token expiry to be handled, so that dictation keeps working across sessions.
25. As the owner, I want the app to send Mandarin-first Doubao recognition requests, so that the first version supports Chinese Mandarin dictation.
26. As the owner, I want English and Mixed-Language Dictation to remain future capabilities, so that the Windows Port can grow after the first Doubao path works.
27. As the owner, I want dictated text inserted into the active application, so that I can speak into browsers, editors, chat apps, and documents.
28. As the owner, I want Clipboard Paste Insertion, so that text insertion works across common Windows applications.
29. As the owner, I want the app not to restore the previous clipboard after insertion, so that paste timing races do not paste stale content.
30. As the owner, I want success to be unobtrusive, so that dictation does not interrupt my workflow.
31. As the owner, I want Minimal Status Feedback, so that I can tell whether the app is idle, recording, transcribing, successful, or in error.
32. As the owner, I want errors to be visible but non-blocking, so that the app does not steal focus from the active target.
33. As the owner, I want a Local Diagnostic Log, so that I can troubleshoot Doubao, hotkey, audio, and paste problems.
34. As the owner, I want an "Open Logs Folder" command, so that I can inspect logs quickly.
35. As the owner, I want logs to avoid storing audio or full transcripts, so that diagnostics remain reasonably private.
36. As the owner, I want no telemetry, so that the Personal Utility stays local and self-contained.
37. As the owner, I want the tray app to quit cleanly, so that hooks, microphone capture, and network sessions are released.
38. As the owner, I want the portable executable to be the artifact used for manual testing, so that testing reflects the real usage shape.
39. As a future maintainer, I want a Windows-specific PRD and ADR trail, so that I do not accidentally implement out-of-scope macOS parity work.
40. As a future Builder, I want deep testable modules, so that the risky system integrations are surrounded by reliable core behavior tests.

## Implementation Decisions

- Build the Windows Port as an independent C#/.NET solution under the Windows context rather than mixing it into the macOS Swift package.
- Use WinForms for the MVP shell: tray icon, tray menu, simple settings window, status changes, and quit behavior.
- Keep the app as a Windows Tray Dictation App, not a macOS-style menu-bar clone and not a full desktop window-first app.
- Deliver the First Usable Windows Version as a Portable Windows Release. Do not build an installer or automatic updater for the MVP.
- Store user configuration and Doubao Device Credentials in a user data location, not beside the portable executable.
- Use a Plain Credential Cache for the MVP. DPAPI or another encrypted credential store is deferred.
- Automatically manage Doubao Device Credentials by porting the existing anonymous device registration and token refresh behavior conceptually from the macOS implementation.
- Implement a Windows-compatible Doubao audio transport adapter. The existing macOS Opus encoder depends on AVFoundation/AudioToolbox and cannot be directly reused on Windows.
- Use a Windows-compatible Opus encoding path, such as a .NET-compatible libopus binding or equivalent, to encode the 16 kHz mono PCM frames required by Doubao.
- Keep the MVP to a Single-Engine Dictation Path using Doubao. Multi-Engine Routing is out of scope.
- Keep the MVP to Mandarin-First Dictation. English recognition and Mixed-Language Dictation are future capabilities.
- Implement Default Microphone Capture using the Windows user's default input device only. No microphone picker, system audio capture, noise suppression feature, or saved recording file is part of the MVP.
- Implement a testable dictation session state machine covering idle, recording, transcribing, inserting, error, and cancellation/cleanup transitions.
- Implement a testable Double-Tap-and-Hold Trigger state machine. The default key is Left Control; a single Control press must not start dictation.
- Use a low-level keyboard hook for the trigger because a standard registered hotkey cannot express double-tap-and-hold push-to-talk semantics.
- Provide trigger configurability in settings, but keep the MVP default as Double-Tap-and-Hold Left Control.
- Implement Clipboard Paste Insertion by writing the final transcript to the clipboard and simulating Ctrl+V into the active application.
- Do not restore the previous clipboard after insertion because restoring can race target application paste handling and paste stale content.
- Keep insertion scoped to normal desktop apps. Do not require support for elevated administrator windows, UAC secure desktop, games, full-screen apps, or remote desktop edge cases in MVP acceptance.
- Provide Minimal Status Feedback through tray icon/menu state rather than a full floating HUD, waveform overlay, or live transcript window.
- Provide Non-Blocking Error Feedback through tray status and notification-level feedback, with details in logs. Avoid modal dialogs except for truly unrecoverable startup failures.
- Write a Local Diagnostic Log. Log trigger transitions, capture start/stop, credential registration/token refresh, Doubao WebSocket lifecycle, frame counts, partial/final receipt, insertion attempts, and errors.
- Do not log audio. Avoid logging full transcript text; log length and at most short previews when useful for diagnostics.
- Provide an "Open Logs Folder" tray command.
- Add optional Startup Shortcut support for the current user. Do not implement a service or machine-wide startup registration.
- Do not include LLM Refinement in the MVP. Keep it as a future capability.
- Do not include Glossary Context in the MVP, but treat it as an early future enhancement after the Doubao path is stable.
- Do not include Multi-Engine Routing, Soniox-first fallback, Apple Speech, or any generic-ASR MVP path.
- Keep the repo's Windows domain language and ADRs authoritative for future work.

Deep modules to build or extract:

- Trigger module: exposes a simple event interface that turns key down/up events into start/stop dictation commands.
- Session controller module: owns dictation lifecycle state and coordinates capture, backend, insertion, status, and logging.
- Doubao credential module: registers anonymous device credentials, loads/saves the Plain Credential Cache, detects expired tokens, and refreshes tokens.
- Doubao protocol module: builds registration/token/session messages and parses Doubao responses without depending on UI.
- Audio capture/encoding module: captures default microphone PCM and produces the frame stream consumed by Doubao transport.
- Text insertion module: abstracts clipboard write and paste simulation behind a small interface for testing.
- Settings module: reads/writes trigger, startup, and credential-related configuration.
- Diagnostics module: provides local logging and log-folder discovery without coupling callers to file paths.
- Tray shell module: adapts application state into WinForms tray UI, settings commands, startup commands, and quit behavior.

## Testing Decisions

- Use the Windows testing workflow documented for the port: unit tests plus portable build manual acceptance. Do not mark Windows changes ready after only `dotnet run`.
- Good tests should cover externally observable behavior through module interfaces, not private implementation details.
- Unit-test the Double-Tap-and-Hold Trigger using synthetic key events and timing boundaries.
- Unit-test the dictation session controller using fake capture, fake Doubao backend, fake insertion, fake clock, and fake logger.
- Unit-test Doubao credential behavior: cache miss, cache hit, expired token refresh, malformed cache, registration/token failure handling.
- Unit-test Doubao message construction and response parsing with fixtures derived from the existing macOS implementation where practical.
- Unit-test JWT expiry detection behavior, following the existing macOS test precedent for credential/config parsing where applicable.
- Unit-test text insertion decision behavior through an abstraction rather than trying to drive the real Windows clipboard in unit tests.
- Unit-test settings persistence behavior with isolated test storage.
- Unit-test log formatting/rotation policy if rotation is implemented.
- Manually accept system integration from the portable release directory: quit running instance, run tests, publish portable build, start the real exe, verify trigger, Control shortcuts, microphone, Doubao transcription, clipboard paste, tray state, quit, and startup shortcut.
- Manual acceptance must verify that normal Control shortcuts are not broken by the default trigger.
- Manual acceptance must verify that releasing the held Control key stops recording.
- Manual acceptance must verify that Mandarin Doubao transcription is inserted into the current focused target.
- Manual acceptance must verify that errors are non-blocking and visible through tray/notification/log behavior.
- Prior art in the repo includes focused Swift unit tests for stateful behavior such as RecordingController, HotkeyMonitor, CancelKeyMonitor, Preferences, language routing, transcript parsing, and backend wiring. The Windows tests should follow the same spirit: isolate state machines and protocol logic from OS integration.

## Out of Scope

- Multi-Engine Routing.
- Soniox-first or generic cloud ASR MVP.
- Apple Speech or Windows built-in speech recognition.
- English-only or full language selection in the MVP.
- Mixed-Language Dictation in the MVP.
- LLM Refinement in the MVP.
- Glossary Context in the MVP.
- Full floating HUD, waveform overlay, live transcript HUD, or draggable status window.
- Microphone picker.
- System audio capture.
- Recording file retention.
- Encrypted credential storage for MVP.
- Installer, MSIX/MSI/Inno/Wix packaging, code signing, SmartScreen mitigation, or automatic updates.
- Telemetry or cloud log upload.
- Elevated administrator window injection support.
- UAC secure desktop support.
- Remote desktop, game, or full-screen app support as acceptance targets.
- Restoring the previous clipboard after insertion.
- Public distribution workflow.

## Further Notes

This PRD follows the Windows Port glossary and the Windows ADRs created during the design session: Doubao-first MVP, .NET implementation stack, WinForms MVP shell, Double-Tap-and-Hold Left Control trigger, and Clipboard Paste Insertion.

The most important product constraint is that the MVP must be Doubao-first. The easiest engineering path, using Soniox or another generic ASR first, was explicitly rejected because it would make the Windows Port lose the product identity of Dousha.

The most important engineering risk is the Windows-compatible Doubao audio/transport adapter, especially replacing the macOS AVFoundation/AudioToolbox Opus encoder with a Windows-compatible Opus path.

The app is a Personal Utility. Several choices that would be unacceptable for a broad consumer product are intentional MVP tradeoffs here: portable packaging, manual updates, Plain Credential Cache, no telemetry, and manual system integration acceptance testing.
