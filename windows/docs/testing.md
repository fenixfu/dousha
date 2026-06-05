# Windows Testing Workflow

The Windows port is validated as a portable app. Do not report Windows changes as ready to test after only `dotnet run` or unit tests; global keyboard hooks, microphone capture, tray lifecycle, clipboard paste insertion, Doubao connectivity, and startup shortcuts must be checked from the portable release directory.

## Required Workflow

1. Quit any running `Dousha.Windows` instance from the tray menu or Task Manager.
2. Run the Windows unit test suite with `dotnet test windows\Dousha.Windows.sln`.
3. Publish a portable build with `powershell -ExecutionPolicy Bypass -File windows\publish.ps1`.
4. Start the real portable executable from `windows\artifacts\Dousha.Windows\Dousha.Windows.App.exe`.
5. Run the manual acceptance checklist below.

## Manual Acceptance Checklist

- Double-tap-and-hold Left Control starts recording only on the second held press.
- Normal Control shortcuts such as copy, paste, save, and tab switching are not broken.
- Releasing the held Control key stops recording.
- Default microphone capture succeeds.
- Doubao transcription returns Chinese Mandarin text.
- The transcript is inserted into the currently focused target through clipboard paste insertion.
- The tray icon/menu reflects idle, recording, transcribing, success, and error states.
- Non-blocking error feedback appears for recoverable trigger, audio, Doubao, or insertion failures, with details written to the Local Diagnostic Log.
- The tray menu can quit the app cleanly.
- In the Settings Window, enabling `Launch at Windows sign-in` creates a current-user Startup Shortcut to the portable executable.
- In the Settings Window, disabling `Launch at Windows sign-in` removes that current-user Startup Shortcut.
- No Windows service, installer, updater, or machine-wide startup registration is created.

## Scope

Core logic should be covered by unit tests where practical. Windows system integration is accepted manually from the portable build because keyboard hooks, focus ownership, microphone devices, and paste targets are environment-dependent.
