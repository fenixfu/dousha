# Windows Testing Workflow

The Windows port is validated as a portable app. Do not report Windows changes as ready to test after only `dotnet run` or unit tests; global keyboard hooks, microphone capture, tray lifecycle, clipboard paste insertion, Doubao connectivity, and startup shortcuts must be checked from the portable release directory.

## Required Workflow

1. Quit any running `Dousha.Windows` instance from the tray menu or Task Manager.
2. Run the Windows unit test suite with `dotnet test`.
3. Publish a portable build into the agreed artifacts directory.
4. Start the real `.exe` from that portable build directory.
5. Run the manual acceptance checklist below.

## Manual Acceptance Checklist

- Double-tap-and-hold Left Control starts recording only on the second held press.
- Normal Control shortcuts such as copy, paste, save, and tab switching are not broken.
- Releasing the held Control key stops recording.
- Default microphone capture succeeds.
- Doubao transcription returns Chinese Mandarin text.
- The transcript is inserted into the currently focused target through clipboard paste insertion.
- The tray icon/menu reflects idle, recording, transcribing, success, and error states.
- The tray menu can quit the app cleanly.
- Startup shortcut creation and removal work when enabled in settings.

## Scope

Core logic should be covered by unit tests where practical. Windows system integration is accepted manually from the portable build because keyboard hooks, focus ownership, microphone devices, and paste targets are environment-dependent.
