# Resolved Handoff: Windows Doubao MVP Live Failure

> [!IMPORTANT]
> This handoff is archived and resolved. The diagnostic instructions and suggested
> next steps below are historical context only. Do not use them as current work
> instructions.

## Resolved

The Windows Doubao core dictation and insertion path passed live acceptance on
**2026-06-06**, resolving the live failure documented in this handoff.

Verified live path:

- Double-tap and hold Left Ctrl started recording.
- The microphone captured Mandarin speech.
- Doubao returned partial and final recognition results.
- The session reached `SessionFinished` and cleaned up successfully.
- Clipboard insertion reached `Inserting -> Success`.
- The Mandarin transcript was pasted into the active text field.

This does not represent completion of the entire GitHub #11 portable acceptance
checklist. Ordinary Ctrl shortcuts, tray quit/error-state behavior, and Startup
Shortcut enable/disable remain separately tracked and require manual acceptance
under #11.

Final root causes and fixes:

1. **Canonical Doubao success code**
   - Doubao protobuf control responses use `20000000`, not HTTP-style `200`.
   - Commit: `e13e0d1` (`canonical 20000000`).

2. **Receive lifecycle**
   - Windows needed a persistent receive loop so early failures, final recognition,
     and authoritative session completion were handled in order.
   - Commit: `334c34f` (`persistent receive loop`).

3. **Stale credential identity cache**
   - Credentials registered before the protocol-profile correction retained
     incompatible `openudid` and `clientudid` shapes.
   - Incompatible cached identities are now invalidated and re-registered.
   - Commit: `3650fa1` (`stale credential cache invalidation`).

4. **Win32 `INPUT` ABI**
   - The incomplete native union produced the wrong `INPUT` size and caused
     `SendInput` paste failure.
   - The complete union now has the required size on x64 and x86.
   - Commit: `9461c01` (`Win32 INPUT ABI`).

The original Doubao dictation/insertion failure is resolved. The historical
notes below are retained to explain the investigation and should not be
interpreted as open work for that path.

## Historical Objective

Continue the PRD #1 Windows MVP work after strict rework of issues #8, #9, and
#10. At the time this handoff was written, the app no longer hung after a Doubao
failure and trigger start/stop worked, but live dictation still reported
`Doubao: transcription failed`.

The original instruction to use `diagnose` and treat fixture success as
insufficient was appropriate then. Live acceptance of the Doubao core
dictation/insertion path has now supplied the missing evidence for this failure.

## Historical User Report

After the strict #8/#9/#10 rework:

- Double-tap Left Ctrl started recording.
- Releasing Left Ctrl sent/stopped recording.
- It no longer got stuck; a later double-tap could start another session.
- It still failed with `Doubao: transcription failed`.

The user stopped that session and requested a handoff.

## Role Context

The original thread operated as **Orchestrator** per `ORCHESTRATOR.md`.
Implementation was delegated to Builder, Tester, and Reviewer subagents.

Relevant project documents:

- `AGENTS.md`
- `PROJECT.md`
- `ORCHESTRATOR.md`
- `BUILDER.md`
- `REVIEWER.md`
- `windows/CONTEXT.md`
- `windows/docs/testing.md`
- `windows/PRD.md`
- `docs/notes/doubao-protocol-notes.md`

## Historical Baseline Commits

The handoff was originally created after:

```text
7a4b960 Stream Doubao audio during Windows dictation
e8d6f19 Align Doubao Windows transport metadata
2f440f4 Rework Doubao protocol parity for Windows
ec2136d Treat Doubao protocol failures as terminal
7ebba53 Add Doubao WebSocket handshake headers
8014808 Add Windows startup shortcut support
66d507a Wire full Doubao dictation path
91a52c5 Prove Windows Doubao audio transport
07092a9 Manage Doubao credentials and protocol messages
```

### Issue #8 Rework

Commit: `2f440f4ee7e1160b51df4dd9ff192a823275b6bf`

- Added full registration query/body identity fields.
- Matched the macOS Doubao User-Agent and identifier shapes.
- Aligned token request shape, `x-ss-stub`, StartSession JSON, and control
  response validation.
- Preserved privacy tests.

### Issue #9 Rework

Commit: `e8d6f19b3bd6532331db7c563d56ae6a269eb71f`

- Added the WebSocket `aid` and `device_id` query parameters.
- Configured keepalive.
- Used epoch-millisecond TaskRequest timestamps.
- Aligned partial tail and silent LAST handling with macOS.
- Retained Concentus for Opus encoding.

### Issue #10 Rework

Commit: `7a4b960c727cb25d8f6a658d51fe8b71f931470c`

- Started streaming on trigger start.
- Fed capture frames while recording.
- Buffered audio until task/session startup completed.
- Sent LAST and FinishSession with bounded waiting.
- Added committed/interim transcript assembly and failure cleanup.

### Hang Fix

Commit: `ec2136d239cb6cf8653873274ef9ef12ae8f0ba8`

- Made `TaskFailed` and `SessionFailed` terminal.
- Added privacy-safe protocol failure metadata.
- Aborted failed control phases before audio transmission.
- Improved trigger command diagnostics.

## Historical Diagnostic Finding

The key log around `2026-06-05T22:43` was:

```text
doubao.protocol.response messageType=TaskStarted statusCode=20000000 resultJsonLength=0
doubao.protocol.failure messageType=TaskStarted statusCode=20000000 statusMessageLength=2 phase=StartTask reason=terminal
```

This correctly identified the first root cause: Windows interpreted Doubao's
canonical protobuf success code, `20000000`, as failure. Commit `e13e0d1`
resolved that error.

Subsequent live testing exposed the receive lifecycle, stale credential cache,
and Win32 `INPUT` ABI issues recorded in the Resolved section.

## Historical Test State

At handoff creation:

- `dotnet test windows\Dousha.Windows.sln`: `73/73` passed.
- `powershell -ExecutionPolicy Bypass -File windows\publish.ps1`: passed.
- `NU1900` warnings occurred because NuGet vulnerability metadata was
  unavailable; restore, build, test, and publish still completed.

Portable executable:

```text
<repo_root>\windows\artifacts\Dousha.Windows\Dousha.Windows.App.exe
```

These results were useful regression evidence but were not live acceptance.
Live acceptance of the Doubao core dictation/insertion path was completed on
2026-06-06 as recorded above; the remaining GitHub #11 manual checks are outside
this resolved handoff.

## Historical Suggested Next Steps: Completed

The original next steps are retained as a completion record:

- [x] Diagnose the latest live failure.
- [x] Confirm the diagnostic log.
- [x] Add regression coverage for canonical `20000000`.
- [x] Correct Doubao response success semantics.
- [x] Run the Windows test suite and portable publish.
- [x] Retest the portable executable manually.
- [x] Trace subsequent failures through `SessionFinished`.
- [x] Invalidate incompatible legacy credential caches.
- [x] Correct the Win32 `INPUT` ABI.
- [x] Verify live Mandarin recognition and clipboard paste.

No stale diagnostic action remains from this handoff.

## Preserved Constraints

- Do not add generic ASR fallback, Soniox-first routing, multi-engine routing,
  English/mixed-language expansion, glossary support, or LLM refinement as part
  of this resolved Doubao core-path work.
- Do not log full transcripts, credentials, tokens, or audio bytes.
- Portable delivery may remain framework-dependent and does not need to bundle
  the .NET Runtime.
