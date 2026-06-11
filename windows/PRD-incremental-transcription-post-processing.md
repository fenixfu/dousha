# PRD: Incremental Transcription Window and Dictation Post-Processing

## Problem Statement

The First Usable Windows Version completes the Core Dictation Experience, but it provides only tray-level state while recording and waits until the end of the recording to insert one final transcript. The user cannot see Doubao's revisable recognition output, cannot tell which text is stable, and cannot reduce final insertion latency by post-processing stable text while speech continues.

Directly revising arbitrary third-party text fields is too fragile for a portable tray utility. Cursor movement, focus changes, input-method composition, asynchronous clipboard paste, and inconsistent UI Automation support can corrupt user text. Dousha therefore needs its own non-activating Transcription Window and Transcript Buffer.

The user also needs Dictation Post-Processing that preserves intended meaning while correcting terminology, maintaining terminology consistency, reducing obvious spoken-expression noise, and resolving explicit self-corrections. It must not turn conversational dictation into formal prose. Processing the entire transcript only after recording ends would add avoidable latency, so stable Post-Processing Units should be processed in one ordered conversation while recording continues.

## Solution

Add a mandatory, non-activating Transcription Window that displays a revisable Transcript Buffer without taking focus from the active text target. Doubao recognition updates feed a Stable Transcript Prefix detector. Stable textual boundaries become ordered Post-Processing Units, while the mutable recognition tail remains visually dimmed.

When configured, one Post-Processing Session is created per recording and sends units sequentially through an Anthropic-compatible Messages API, initially targeting DeepSeek V4 Flash with thinking disabled. Earlier source units and results remain in the conversation history, preserving terminology and context while benefiting from stable-prefix caching. Successful results replace their source units in place and render in bold.

The Windows Doubao session should disable pause-driven finalization while retaining long-utterance repetition guards. It must not enable Doubao's end-only text post-processing because that would prevent incremental Dictation Post-Processing.

At recording completion, the window first waits for Doubao's complete final ASR result under the transport's existing two-second finish timeout. A finish timeout is an explicit dictation failure and does not insert partial text. The remaining tail then becomes a final unit and the app drains the ordered queue. Every provider request has a 20-second transport timeout. The terminal turn is the last unit request when no reconciliation is needed, or the Final Transcript Reconciliation request when one is needed. Its three-second insertion deadline is measured from the instant that terminal request is sent, even if that occurs before recording release; an already-expired deadline causes immediate insertion once finalization reaches that state. The user may request Immediate Insertion with a plain double-tap of Left Control while waiting, or cancel the session with Escape.

## User Stories

1. As the owner, I want to see live recognition while speaking, so that I know Dousha is hearing the intended words.
2. As the owner, I want live recognition shown outside the target application, so that interim corrections cannot corrupt my document.
3. As the owner, I want the Transcription Window not to take focus, so that the active text target remains ready for final insertion.
4. As the owner, I want the Transcription Window to be part of every dictation, so that progress and recovery behavior are consistent.
5. As the owner, I want mutable recognition text shown dimmed, so that I know it may still change.
6. As the owner, I want frozen text shown in regular weight, so that I know it is queued or awaiting post-processing.
7. As the owner, I want successfully post-processed text shown in bold, so that I can see completed progress.
8. As the owner, I want unchanged but successfully processed text shown in bold, so that bold consistently means completion.
9. As the owner, I want post-processing failures to remain as regular text without extra warnings, so that the transcript stays readable.
10. As the owner, I want the window title to distinguish dictating, waiting for final transcription, waiting for post-processing, insertion, success, recoverable paste failure, unrecoverable workflow failure, and cancellation, so that I understand what the app is doing.
11. As the owner, I want status labels in Chinese for the first version, so that the primary workflow remains concise.
12. As the owner, I want the title to avoid shortcut instructions, so that the compact layout remains uncluttered.
13. As the owner, I want the window fixed-width and bottom-centered, so that its position is predictable.
14. As the owner, I want the window bottom flush with the taskbar work area, so that it uses screen space efficiently.
15. As the owner, I want the body to grow upward to five lines, so that short dictations remain compact.
16. As the owner, I want long dictations to scroll while following the latest text, so that the newest recognition remains visible.
17. As the owner with multiple displays, I want the window placed on the foreground application's display at recording start, so that feedback appears near my work.
18. As the owner, I want the display choice fixed for one recording, so that the window does not jump between screens.
19. As the owner, I want approximately 18 logical-pixel body text in a roughly 600 logical-pixel window, so that Chinese text is comfortably readable.
20. As the owner, I want thinking pauses not to finalize utterances, so that pauses do not create false sentence boundaries.
21. As the owner, I want textual stability rather than pauses to freeze units, so that incremental processing remains semantically grounded.
22. As the owner, I want a boundary stable across two updates for 500 milliseconds with trailing text before it freezes, so that late ASR revisions are unlikely.
23. As the owner, I want hard punctuation to create Post-Processing Units, so that complete statements begin processing early.
24. As the owner, I want a long unit of at least 60 characters to split at a suitable comma with at least 30 characters before it, so that long speech can process incrementally.
25. As the owner, I do not want a forced hard character limit, so that unusual long expressions preserve semantic integrity.
26. As the owner, I want a punctuation-free trailing phrase submitted when recording ends, so that short commands and phrases are still processed.
27. As the owner, I want frozen units never rolled back, so that the ordered model conversation remains coherent.
28. As the owner, I want substantive final-ASR conflicts reconciled as a complete transcript, so that late recognition improvements are not lost.
29. As the owner, I do not want reconciliation for whitespace or equivalent punctuation differences, so that normal dictations avoid extra latency and token use.
30. As the owner using raw transcription, I want the complete final ASR result to replace the visual buffer before insertion, so that a disabled post-processor does not require model reconciliation.
31. As the owner, I want one Post-Processing Session per recording, so that context is useful without leaking between unrelated dictations.
32. As the owner, I want units processed in order, so that terminology and references remain coherent.
33. As the owner, I want lossless queueing when the model is slower than speech, so that no stable text is discarded or merged.
34. As the owner, I want later units to continue after one unit fails, so that a transient error does not disable the rest of the recording.
35. As the owner, I want a failed unit represented by its source text in conversation history, so that later units retain complete context.
36. As the owner, I want conservative post-processing by default, so that my conversational voice is preserved.
37. As the owner, I want obvious terminology errors and inconsistent terminology corrected, so that technical dictation is accurate.
38. As the owner, I want explicit self-correction scaffolding removed, so that the final sentence retains only my corrected meaning.
39. As the owner, I want a single "嗯" or "呃" preserved, so that normal conversational cadence is not erased.
40. As the owner, I want repeated short hesitation tokens collapsed to one, so that accidental repetition is reduced.
41. As the owner, I want discourse markers such as "就是说" retained in conservative mode unless clearly accidental, so that chat input is not over-edited.
42. As the owner, I want aggressive intensity visible only as a future placeholder, so that the initial behavior is not underspecified.
43. As the owner, I want the application to own the post-processing policy, so that the ordered protocol cannot be broken by free-form prompt edits.
44. As the owner, I want Anthropic-compatible DeepSeek configuration, so that the intended post-processing provider is supported directly.
45. As the owner, I want thinking explicitly disabled, so that post-processing prioritizes low latency.
46. As the owner, I want one active provider configuration, so that the first settings experience remains simple.
47. As the owner, I want configurable endpoints restricted to HTTPS, so that credentials and transcripts are not sent over plaintext transport.
48. As the owner, I want my API key stored in Windows Credential Manager, so that it is not written to the settings file.
49. As the owner, I want Dousha to own its credential entry, so that it does not depend on an externally named credential.
50. As the owner, I want saving the credential to enable post-processing automatically, so that setup has no redundant confirmation.
51. As the owner, I want to disable post-processing without deleting the credential, so that I can temporarily use raw transcription.
52. As the owner, I want deleting the credential to disable post-processing, so that configuration remains internally consistent.
53. As the owner, I want a disclosure that transcript text is sent to the configured service, so that the settings behavior is explicit.
54. As the owner, I want connection testing to be advisory, so that temporary network failure does not erase or disable saved configuration.
55. As the owner, I want runtime post-processing errors to fall back silently to source text, so that dictation still completes.
56. As the owner, I want every provider request bounded by a transport timeout, so that a broken connection cannot wait forever.
57. As the owner, I want the terminal request timed from when it is actually sent, so that long recordings do not deprive the last turn of processing time.
58. As the owner, I want a three-second deadline for the terminal post-processing request, so that final insertion has a bounded last-stage wait.
59. As the owner, I want a late result after system timeout to replace the clipboard only if I have not changed it and no newer recording owns it, so that the improved result remains usable without destroying newer content.
60. As the owner, I want a plain double-tap of Left Control while waiting to insert immediately, so that I can skip remaining processing.
61. As the owner, I want Immediate Insertion to keep completed results and use source text for unfinished units, so that the best current transcript is inserted.
62. As the owner, I want Immediate Insertion to cancel pending work and discard late callbacks, so that abandoned results never alter the clipboard.
63. As the owner, I want Escape during recording or waiting to cancel the whole dictation, so that I can discard unwanted speech.
64. As the owner, I want cancellation to clear the body and show "已取消" for about 1.5 seconds, so that the action is confirmed.
65. As the owner, I want cancellation to leave the clipboard untouched, so that unrelated clipboard contents are preserved.
66. As the owner, I want insertion to target whichever text field is active at insertion time, so that intentional focus changes are respected.
67. As the owner, I want the title to show "文本粘贴中" and operational "粘贴成功", so that clipboard-write and paste-dispatch progress is visible.
68. As the owner, I want successful insertion to fade after about 300 milliseconds, so that feedback is visible but unobtrusive.
69. As the owner, I want failed insertion to preserve the complete transcript on the clipboard, so that I can paste manually.
70. As the owner, I want paste failure shown fully for about 1.5 seconds and then collapsed to the title, so that the target field is less obstructed.
71. As the owner, I want paste failure to block a new recording until acknowledged, so that Dousha does not overwrite its recovery clipboard.
72. As the owner, I want the next double-tap after paste failure to acknowledge and close the failure state without recording, so that recovery is intentional.
73. As the owner, I want dictation failure to show "听写失败，请查看日志" for about 1.5 seconds, so that protocol failures remain diagnosable.
74. As the owner, I want the next trigger after dictation failure to start recording immediately, so that failures do not unnecessarily block retry.
75. As the owner, I want logs to exclude transcripts, API keys, audio, HTTP headers, request bodies, and response bodies, so that diagnostics do not expose dictated content.
76. As the owner, I want final-ASR waiting bounded by the existing two-second finish timeout, so that a broken Doubao session cannot leave the window waiting forever.

## Implementation Decisions

- Keep the Windows Port as a portable WinForms tray application and preserve Normal-Privilege Operation.
- Add the Doubao session parameters `enable_vad_timeout_break=false`, `no_repeat_ngram_size=6`, and `max_indefinite_utterance=1`.
- Do not enable `enable_text_post_process` or `last_post_process`; Doubao Text Post-Processing is distinct from Dictation Post-Processing.
- Treat rejection or pathological behavior from the three-parameter guard set as an explicit dictation failure. Do not retry with the legacy configuration.
- Introduce a streaming recognition snapshot contract that exposes the cumulative recognized text needed by the Transcript Buffer.
- Build a Stable Transcript Prefix module that compares recognition snapshots and freezes textual boundaries only after two updates, 500 milliseconds, and newer trailing text.
- Build a Post-Processing Unit segmenter over Unicode text elements. Hard boundaries are `。`, `！`, `？`, `!`, `?`, `；`, and `;`, and the punctuation belongs to the preceding unit. Extract every complete hard-boundary unit available in one snapshot.
- Apply the soft boundary only when the unfrozen buffer contains at least 60 text elements. Choose the latest `，` or `,` for which the prefix including that comma contains at least 30 text elements; the comma belongs to the preceding unit. If no comma qualifies, wait for more text. Do not add a hard maximum.
- Make frozen source units append-only. Later recognition updates may revise only the unfrozen tail.
- Build a Transcript Buffer module that owns ordered source units, post-processed replacements, the mutable tail, and visual state independent of WinForms.
- Build a Post-Processing Session module with a small ordered interface: enqueue a source unit, receive a unit result, reconcile a final conflict, cancel by generation, and assemble the best current transcript.
- Use one ordered conversation per recording. Each request includes the full accumulated user and assistant turns so the provider can preserve context and reuse cached prefixes.
- Do not truncate, summarize, or silently restart the conversation when it approaches the provider context limit. The first version supports recordings that fit the configured provider context. A provider context-limit rejection follows normal unit-failure behavior: retain source text as the local assistant turn and continue attempting later units.
- When a unit request fails, store the source text as the local assistant turn, leave the UI unit in regular weight, and continue with later requests.
- Apply a 20-second transport timeout to every provider request. A timed-out intermediate request fails to source text and allows the ordered queue to advance.
- After recording release, wait for the complete final ASR result before deciding the final tail and reconciliation requirement. Reuse the Doubao transport's existing two-second finish timeout. A finish timeout is an explicit dictation failure, discards partial ASR text, does not modify the clipboard, and permits the next recording trigger.
- Previously queued post-processing requests continue under their transport timeouts; there is no separate post-release global deadline.
- Define the terminal turn as the last unit request when reconciliation is unnecessary, or the Final Transcript Reconciliation request when reconciliation is required. Track its three-second insertion deadline from its actual send timestamp. If it was sent before release and the deadline has already elapsed when finalization reaches the waiting state, insert immediately.
- A three-second terminal deadline does not cancel the request. Insert the best current transcript, allow the request to continue within its 20-second transport timeout, and apply the guarded deferred clipboard update only if it later completes successfully.
- Build Final Transcript Reconciliation as a conditional final turn in the same session. Provide the complete final ASR result and the assembled incremental result and request one complete reconciled transcript.
- Detect final conflict after normalizing whitespace and equivalent full-width or half-width punctuation. Lexical, numeric, or other substantive changes inside the frozen source range trigger reconciliation.
- Skip reconciliation on compatible final results.
- When Dictation Post-Processing is disabled, bypass frozen-prefix reconciliation. The complete final ASR result is authoritative and replaces the visual Transcript Buffer before insertion.
- Build an Anthropic-compatible Messages API adapter. The initial defaults are DeepSeek's Anthropic-compatible endpoint and `deepseek-v4-flash`, with thinking disabled.
- Support one Post-Processing Configuration only: enabled state, HTTPS endpoint, model, conservative intensity, and one API credential. Reject non-HTTPS endpoint values before saving or testing the configuration.
- Store the API key in the current user's Windows Credential Manager under the stable Dousha-owned target `Dousha/PostProcessing/ApiKey`.
- Store non-secret configuration in the normal settings store. Never serialize the API key into settings or logs.
- Saving a credential enables post-processing by default. The user may later disable it without deleting the credential. Deleting the credential disables post-processing.
- Keep the Post-Processing Policy application-owned and non-editable.
- Implement conservative policy only. Show aggressive intensity as disabled or "coming later" until its policy is designed.
- Preserve a single short hesitation, collapse repeated short hesitations to one, preserve normal discourse markers, remove explicit self-correction scaffolding, correct terminology, and avoid stylistic rewriting.
- Keep a future-compatible empty Glossary Context input boundary, but do not build glossary maintenance.
- Build a recording workflow coordinator that combines recognition, Stable Transcript Prefix, Transcript Buffer, Post-Processing Session, insertion, cancellation, timeout, and recovery state.
- Extend trigger routing by workflow state:
  - Recording uses Double-Tap-and-Hold Left Control and stops on release.
  - Waiting for post-processing uses a consumed plain double-tap for Immediate Insertion.
  - Recording and waiting consume Escape for Dictation Cancellation.
  - Pasting and success ignore trigger double-taps.
  - Paste Failure Recovery consumes the next double-tap as acknowledgement only.
  - Dictation failure does not block the next recording trigger.
- Cancel in-flight and queued post-processing on Immediate Insertion, resolve unfinished units to source text, invalidate the session generation, then insert the best current transcript.
- On system timeout, insert the best current transcript but allow the pending result to finish. Record both the Windows clipboard sequence number immediately after the timeout insertion and the current Dousha recording generation.
- A late completion writes the complete final post-processed transcript to the clipboard without reopening the window only when the clipboard sequence number is unchanged and no newer Dousha recording has written the clipboard. Otherwise discard the late result and log metadata only.
- Build the Transcription Window as a non-activating WinForms window that never accepts text editing focus.
- Use the following complete window-state contract:

| Workflow state | Fixed title | Exit condition |
| --- | --- | --- |
| Recording | `听写中` | Recording key released or cancellation |
| Waiting for final ASR | `等待听写完成` | Complete final ASR arrives, two-second finish timeout or another dictation failure occurs, or cancellation |
| Waiting for post-processing | `等待后处理完成` | Queue completes, terminal deadline expires, Immediate Insertion, or cancellation |
| Pasting | `文本粘贴中` | Clipboard write and paste dispatch succeed, clipboard write fails, or paste dispatch fails |
| Paste succeeded | `粘贴成功` | Approximately 300-millisecond fade completes |
| Paste failed | `粘贴失败，请查看剪贴板` | User acknowledges after full-window display and title-only collapse |
| Unrecoverable workflow failure | `听写失败，请查看日志` | Approximately 1.5-second fade or a new recording replaces it |
| Cancelled | `已取消` | Approximately 1.5-second fade or a new recording replaces it |

- Render mutable interim text dimmed, frozen source text in regular weight, and every successfully processed unit in bold.
- Use an approximately 600 logical-pixel fixed width, approximately 18 logical-pixel body text, five body lines before scrolling, and DPI-aware sizing.
- Position the window bottom-center and flush with the taskbar work area. Select the foreground application's display at recording start, fall back to the pointer display, and do not move during the recording.
- Keep insertion targeted at the active text target at insertion time. Do not restore the recording-start application.
- Define operational paste success as a successful clipboard write plus successful dispatch of all required `SendInput` events. It does not claim that the target application accepted or rendered the text, which is not portably observable.
- On operational paste success, show success for approximately 300 milliseconds and fade out.
- When the clipboard write succeeds but `SendInput` dispatch is incomplete, enter Paste Failure Recovery: keep the complete transcript on the clipboard, show the full window for approximately 1.5 seconds, collapse to the title, and guard the next trigger until acknowledgement.
- When the clipboard write fails, no recovery clipboard exists. Enter the same transient unrecoverable workflow-failure path used by dictation failure: show `听写失败，请查看日志` for approximately 1.5 seconds, do not guard the next trigger, and record metadata-only diagnostics.
- On dictation failure, do not paste or modify the clipboard. Show the unrecoverable workflow-failure status for approximately 1.5 seconds, then fade; a new recording may replace it immediately.
- On cancellation, clear the body, show `已取消` for approximately 1.5 seconds, do not modify the clipboard, and allow immediate new recording.

Deep modules to build or extract:

- Stable Transcript Prefix tracker and Post-Processing Unit segmenter.
- Transcript Buffer and presentation-state model.
- Ordered Post-Processing Session and final reconciliation coordinator.
- Anthropic-compatible post-processing client.
- Windows Credential Manager credential store.
- Recording workflow and trigger-state coordinator.
- Transcription Window presenter/view boundary.
- Paste and clipboard recovery coordinator.

## Testing Decisions

- Test externally observable behavior through module interfaces rather than private implementation details.
- Unit-test Stable Transcript Prefix behavior with synthetic snapshots and a fake clock: two-update requirement, 500-millisecond requirement, trailing-text requirement, the exact hard-punctuation set, extraction of multiple hard units, Unicode text-element counting, latest-eligible-comma selection, 60/30 thresholds, punctuation ownership, no hard maximum, final-tail flush, and frozen-prefix immutability.
- Unit-test Transcript Buffer rendering state: dimmed interim, regular frozen/failed units, bold successful units including unchanged results, replacement order, tail revision, and full reconciliation.
- Unit-test Post-Processing Session ordering with a fake Anthropic client: full accumulated conversation history, lossless queueing, source-as-assistant continuation after failure, context-limit rejection behavior, generation cancellation, and no late callback effects.
- Unit-test conservative policy using representative Chinese dictation examples: terminology correction, repeated hesitation collapse, single hesitation preservation, discourse-marker preservation, and explicit self-correction removal.
- Unit-test final conflict normalization and reconciliation triggering, including whitespace and full-width/half-width punctuation equivalence.
- Unit-test final timing with a fake clock: two-second Doubao finish timeout, 20-second intermediate provider transport timeout, terminal send before and after recording release, already-expired terminal deadline, reconciliation as the terminal turn, Immediate Insertion cancellation, and successful or failed guarded deferred clipboard updates.
- Unit-test clipboard ownership using fake clipboard sequence numbers and recording generations, including user clipboard changes and newer Dousha recordings.
- Unit-test Credential Manager behavior behind an abstraction: save, read, replace, delete, missing credential, and settings enablement transitions. Do not require the real user vault in ordinary unit tests.
- Unit-test trigger routing and title transitions across recording, waiting for final ASR, waiting for post-processing, pasting, success, paste failure, dictation failure, and cancellation states.
- Unit-test operational paste classification: clipboard failure enters transient unrecoverable failure without trigger guard, partial `SendInput` dispatch enters guarded Paste Failure Recovery, complete dispatch reports operational success, and no path claims that target acceptance was observed.
- Unit-test window placement and presentation calculations independently from native rendering: five-line cap, DPI scaling inputs, taskbar work area, foreground-display selection, pointer fallback, and fixed-per-recording display.
- Unit-test Doubao session configuration contains the three guard parameters and excludes end-only text post-processing.
- Unit-test Post-Processing Configuration rejects non-HTTPS endpoints.
- Unit-test diagnostic logging redaction so API keys, authorization and provider headers, transcripts, audio, request bodies, and response bodies never enter logs.
- Extend session-controller tests with streaming snapshots, post-processing, cancellation, timeout, insertion, clipboard recovery, and explicit dictation failure.
- Follow existing prior art in the Windows tests for trigger state machines, session orchestration, protocol construction, transport fakes, settings persistence, and clipboard insertion.
- Run `dotnet test windows\Dousha.Windows.sln` before manual acceptance.
- Publish and test the real portable executable using the documented Windows testing workflow.
- Manual acceptance must cover:
  - Live dimmed recognition, regular frozen units, and bold processed units.
  - Thinking pauses without false unit finalization.
  - Hard and soft unit boundaries.
  - Short punctuation-free dictation.
  - Long dictation with queued post-processing.
  - Immediate Insertion and Escape cancellation.
  - Final request timeout and deferred complete clipboard update.
  - Raw mode replacing the visual buffer with complete final ASR before insertion.
  - Paste success, paste failure collapse/acknowledgement, and dictation failure retry.
  - Focus changes before insertion.
  - Multiple displays and DPI scaling.
  - Credential Manager save/delete and post-processing enable/disable.
  - Portable artifact operation rather than only `dotnet run`.

## Out of Scope

- Directly streaming and revising text inside arbitrary third-party text fields.
- Editable Transcription Window content.
- A setting to disable the Transcription Window.
- Shortcut hints in the Transcription Window title.
- Transcription Window localization.
- OpenAI-compatible post-processing.
- Multiple provider profiles or provider switching.
- User-editable system prompts.
- Aggressive post-processing policy design or implementation.
- Glossary document format, editing, synchronization, or maintenance UX.
- Non-empty Glossary Context management.
- Doubao `last_post_process` or `stream_post_process`.
- Legacy Doubao configuration fallback after guard-set failure.
- Cross-recording Post-Processing Sessions.
- Per-unit user-experience deadlines while recording; the 20-second HTTP transport timeout remains in scope.
- Routine whole-transcript reconciliation when there is no conflict.
- Installer, updater, public distribution, elevated-window insertion, or clipboard restoration.
- The independent Swift Windows port in the research repository.

## Further Notes

- This PRD follows the Windows Port glossary in `windows/CONTEXT.md`.
- The no-pause-finalization guard set is derived from reverse-engineering in giraphant/dousha PR #24, but that PR validated the three guards only together with end-only Doubao Text Post-Processing. The Windows implementation must therefore treat real-device validation of the three-parameter subset as an explicit acceptance risk.
- Doubao Text Post-Processing and Dictation Post-Processing are intentionally separate terms. The former is ASR finalization/re-punctuation; the latter is the ordered language-model conversation described here.
- The Transcription Window is the buffer visualization and recovery surface. The buffer itself remains a testable core model independent of WinForms.
- The first conservative policy should be treated as a versioned application contract with tests, not as an informal prompt string.
