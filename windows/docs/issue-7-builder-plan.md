# Issue #7 Builder Plan: Capture Default Microphone Audio

## Scope

Implement Default Microphone Capture for the Windows Port only. The capture path will use the Windows user's default input device, expose captured PCM audio as frames for downstream Doubao encoding/transport, and remain owned by the existing dictation session controller lifecycle.

Out of scope: microphone picker, system audio capture, noise suppression, saved recording files, clipboard/text insertion work.

## Assumptions

- The public stream shape should be simple PCM frame data plus format metadata.
- Unit tests should verify lifecycle, frame shape, and error behavior through interfaces/fakes.
- Real microphone access remains manual acceptance from the portable build because device availability is environment-dependent.

## Implementation Plan

1. Add a focused audio frame model to `Dousha.Windows.Core` and update `CapturedAudio` to carry frame data and format metadata.
2. Extend session-controller tests so downstream backend fakes receive captured frames, while existing capture start/stop error paths keep surfacing Non-Blocking Error Feedback and Local Diagnostic Log entries.
3. Add a Windows app capture implementation backed by the default input device only.
4. Wire the app project/package references only as needed for microphone capture.
5. Run `dotnet test windows\Dousha.Windows.sln`; if publish surface changes, run `powershell -ExecutionPolicy Bypass -File windows\publish.ps1`.

## Test Plan

- Unit: `CapturedAudio` exposes byte counts from frames without relying on real microphone hardware.
- Unit: `DictationSessionController` passes captured frame stream to backend and logs captured byte count.
- Unit: capture start/stop failures produce audio-area non-blocking feedback and diagnostic log entries.
- Build gate: `dotnet test windows\Dousha.Windows.sln`.
- Publish gate: `powershell -ExecutionPolicy Bypass -File windows\publish.ps1`.

## Review Handoff

Builder will hand this back to the Orchestrator for gateway review and any Tester/Reviewer delegation after implementation.
