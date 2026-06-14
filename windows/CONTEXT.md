# Windows Port

The Windows Port context defines the product language for Dousha's Windows edition and separates the user-facing dictation experience from platform-specific integrations.

## Language

### WSL Desktop Paste

**WSL Desktop Paste Target**:
A Windows foreground window that hosts a Linux Xwayland surface, recognized by a configurable window-title prefix.
_Avoid_: WSL window, Linux window, X11 window

**Xwayland Paste Shortcut**:
The configurable key sequence DoushaWin sends to a WSL Desktop Paste Target instead of the standard paste shortcut.
_Avoid_: Alt+V, Linux paste key, special paste shortcut

**Foreground Paste Detection**:
Inspecting the active window title at injection time to choose the appropriate paste shortcut for the current target.
_Avoid_: process-based detection, recording-start detection, target caching

### Windows Port

**Windows Port**:
The Windows edition of Dousha that preserves the core dictation experience while using Windows-native integrations.
_Avoid_: Direct Swift port, macOS clone

**Core Dictation Experience**:
A flow where the user triggers recording, speaks, receives a transcript, and the resulting text appears in the active text target.
_Avoid_: Feature parity, full clone

**Post-Processing Strategy Evaluation**:
A controlled comparison of Doubao Text Post-Processing and Dictation Post-Processing by recognition speed, transcript accuracy, and lived dictation experience before choosing a Windows implementation.
_Avoid_: .NET versus Swift benchmark, feature-count comparison, settings comparison

**Windows Tray Dictation App**:
A background Windows app surfaced from the notification area for settings and status.
_Avoid_: Menu-bar app, Dock app

**Settings Window**:
A lightweight Windows window where users configure dictation behavior and credentials.
_Avoid_: Preferences pane, settings page

**Minimal Status Feedback**:
Tray-level recording, transcribing, success, and error feedback without a full floating dictation HUD.
_Avoid_: Full HUD, waveform overlay, live transcript window

**Transcription Window**:
A non-activating floating window that visualizes the revisable transcript buffer and post-processing progress without taking focus from the active text target.
_Avoid_: Text editor, focused dialog, tray-only status

**Transcript Buffer**:
The ordered recording text whose interim tail may be revised, whose frozen source units await post-processing, and whose processed units replace their source text in place.
_Avoid_: Editable document, target text field

**Transcription Window Status**:
The recording-level state shown in the Transcription Window title bar: dictating, waiting for final transcription, waiting for post-processing, pasting, paste succeeded, paste failed with clipboard recovery, unrecoverable workflow failure, or cancelled.
_Avoid_: Sentence processing style, tray status

**Paste Failure Recovery**:
A guarded state that preserves the complete transcript on the clipboard, temporarily shows the full Transcription Window, then collapses it to the failure title until the user acknowledges it.
_Avoid_: Automatic retry, modal dialog, immediate dismissal

**Transcription Window Layout**:
A fixed-width bottom-centered layout that grows upward to five body lines, then scrolls to keep the latest transcript visible.
_Avoid_: Full-screen transcript, unbounded growth, bottom margin

**First Usable Windows Version**:
The smallest Windows release that proves the hotkey, microphone capture, cloud transcription, and text insertion loop works end to end.
_Avoid_: Full parity release, complete port

**Single-Engine Dictation Path**:
A dictation flow where one selected transcription engine produces the final text for a recording.
_Avoid_: Multi-engine routing

**Doubao Dictation Path**:
A dictation flow where Doubao is the transcription engine that produces the final text for a recording.
_Avoid_: Generic ASR MVP, Soniox-first MVP

**Doubao Device Credentials**:
The anonymous device identity and token required to use the Doubao dictation service.
_Avoid_: Manual token, user login

**Plain Credential Cache**:
A local credential cache that stores Doubao device credentials without per-user encryption for the MVP.
_Avoid_: DPAPI credential vault, encrypted token store

**Post-Processing API Credential**:
A user-provided language-model secret stored for the current user in Windows Credential Manager.
_Avoid_: Settings file, Plain Credential Cache, diagnostic log

**Post-Processing Configuration**:
The single active set of post-processing enablement, HTTPS endpoint, model, and Dousha-owned API credential.
_Avoid_: Provider profiles, configuration switching

**Anthropic-Compatible Post-Processing**:
The Messages API protocol used by the Post-Processing Session, initially targeting DeepSeek with thinking disabled.
_Avoid_: OpenAI-compatible protocol, provider selector

**Post-Processing Policy**:
The application-owned rules that govern the ordered unit protocol and permitted transcript transformations.
_Avoid_: User-editable system prompt, stylistic preset

**Post-Processing Intensity**:
The configured degree to which Dictation Post-Processing may remove spoken discourse elements while preserving meaning and voice.
_Avoid_: Writing style, model temperature

**Default Microphone Capture**:
Audio capture from the Windows user's default input device, converted into the format required by Doubao.
_Avoid_: Microphone picker, system audio capture, saved recordings

**Portable Windows Release**:
A Windows build distributed as files the user can unzip and run without an installer.
_Avoid_: MSI, MSIX, installer-first release

**Personal Utility**:
A tool built for the owner's own use rather than broad public distribution.
_Avoid_: Public consumer product, managed enterprise app

**Startup Shortcut**:
A current-user Windows startup entry implemented as a shortcut to the portable app.
_Avoid_: system service, machine-wide startup registration

**Double-Tap-and-Hold Trigger**:
A push-to-talk trigger where the user taps a modifier key once, then quickly presses and holds it a second time to record until release.
_Avoid_: Toggle, single-tap hotkey

**Clipboard Paste Insertion**:
A text insertion strategy that writes dictated text to the clipboard and posts a paste command to the active text target.
_Avoid_: Keystroke typing, clipboard restore

**Mandarin-First Dictation**:
A dictation language scope where the first release targets Simplified Chinese Mandarin before adding English or mixed-language recognition.
_Avoid_: Full language routing, English-first dictation

**Mixed-Language Dictation**:
A dictation capability where one recording may contain both Chinese and English speech.
_Avoid_: Multi-engine routing

**Dictation Post-Processing**:
A language-model step that corrects and standardizes terminology and removes dictated filler without rewriting the user's meaning or style.
_Avoid_: LLM refinement, cleanup, stylistic rewriting, core transcription, ASR engine

**Doubao Text Post-Processing**:
An ASR-server pass that changes utterance finalization and semantically re-punctuates recognized text.
_Avoid_: Dictation Post-Processing, terminology correction, discourse editing

**Post-Processing Session**:
A conversation-scoped sequence of ordered unit transformations that lasts for exactly one recording.
_Avoid_: Independent sentence requests, cross-recording conversation

**Post-Processing Unit**:
An ordered transcript fragment submitted to a Post-Processing Session after a hard sentence boundary, an eligible soft comma boundary, or recording completion.
_Avoid_: Sentence, ASR segment, interim text

**Stable Transcript Prefix**:
The leading text preserved across recognition updates and followed by newer text, from which Post-Processing Units may be frozen before ASR finalization.
_Avoid_: Finalized ASR segment, current interim tail

**Final Transcript Reconciliation**:
A conditional final Post-Processing Session turn that reconciles the complete ASR result with the assembled incremental post-processing result when their frozen prefixes conflict.
_Avoid_: Routine whole-transcript rewrite, character-level merge

**Immediate Insertion**:
A user command that inserts the best currently available transcript and abandons unfinished post-processing for that recording.
_Avoid_: Post-processing timeout, deferred clipboard update

**Dictation Cancellation**:
An active-session Escape command that discards the recording, Transcript Buffer, and Post-Processing Session without insertion or clipboard update.
_Avoid_: Immediate Insertion, paste failure acknowledgement

**Glossary Context**:
User-provided terms sent as recognition context to improve transcription of domain words and names.
_Avoid_: Dictation Post-Processing, language routing

**Local Diagnostic Log**:
A local file log for troubleshooting dictation, integration, and Doubao failures without telemetry.
_Avoid_: telemetry, audio recording, cloud log upload

**Non-Blocking Error Feedback**:
Tray-level or notification-level error feedback that records details in logs without interrupting the active app with modal dialogs.
_Avoid_: modal error dialog, focus-stealing alert

**Normal-Privilege Operation**:
Running the Windows tray app as a standard user process without elevation.
_Avoid_: administrator-required app, elevated tray process

**Multi-Engine Routing**:
A dictation flow where multiple transcription engines receive the same recording and one engine's whole-result transcript is selected after language classification.
_Avoid_: Per-sentence routing, transcript merging

## Relationships

- The **Windows Port** preserves the **Core Dictation Experience**.
- **Clipboard Paste Insertion** uses **Foreground Paste Detection** to decide whether the active target is a **WSL Desktop Paste Target**.
- A **WSL Desktop Paste Target** receives the configured **Xwayland Paste Shortcut** instead of the standard paste shortcut.
- **Foreground Paste Detection** inspects the active window title at injection time and falls back to the standard paste shortcut when the title cannot be read.
- A **Post-Processing Strategy Evaluation** precedes the choice of the long-term Windows implementation.
- A **Post-Processing Strategy Evaluation** compares **Doubao Text Post-Processing** with **Dictation Post-Processing** independently of whether the implementation uses .NET or Swift.
- The **Windows Port** is delivered as a **Windows Tray Dictation App**.
- A **Windows Tray Dictation App** exposes configuration through a **Settings Window**.
- A **Windows Tray Dictation App** provides **Minimal Status Feedback** in the **First Usable Windows Version**.
- A future **Transcription Window** preserves the active text target while displaying live transcription and post-processing state.
- The **Transcription Window** renders the **Transcript Buffer** with interim text dimmed, frozen source text in regular weight, and post-processed text in bold.
- Every successfully post-processed unit uses bold text even when its returned content is identical to the source.
- Failed post-processing remains as regular-weight frozen source text without additional error decoration.
- The **Transcription Window** remains available when **Dictation Post-Processing** is disabled; in that mode it shows interim and frozen transcription only.
- The **Transcription Window** is a required interaction surface and has no disable setting.
- The **Transcription Window** title bar shows the current **Transcription Window Status**.
- The **Transcription Window** title bar contains status text only and does not include shortcut hints.
- The first **Transcription Window Status** labels are fixed Chinese strings: `听写中`, `等待听写完成`, `等待后处理完成`, `文本粘贴中`, `粘贴成功`, `粘贴失败，请查看剪贴板`, `听写失败，请查看日志`, and `已取消`; localization is deferred.
- A successful paste keeps the **Transcription Window** visible for approximately 300 milliseconds before it fades out.
- **Paste Failure Recovery** keeps the full window visible for approximately 1.5 seconds, then collapses it to the title bar.
- While **Paste Failure Recovery** is active, a Double-Tap-and-Hold Trigger cannot begin recording; the next double-tap acknowledges and closes the failure state without starting a recording.
- The **Transcription Window Layout** places the window directly against the top edge of the taskbar work area.
- At recording start, the **Transcription Window Layout** selects the display containing the foreground application, falls back to the pointer display, and remains on that display for the recording.
- The **First Usable Windows Version** uses the **Doubao Dictation Path**.
- The **Doubao Dictation Path** is a **Single-Engine Dictation Path**.
- The **Doubao Dictation Path** requires **Doubao Device Credentials**.
- The **First Usable Windows Version** stores **Doubao Device Credentials** in a **Plain Credential Cache**.
- A **Post-Processing API Credential** is distinct from **Doubao Device Credentials** and must use Windows Credential Manager.
- Dousha owns a dedicated Windows Credential Manager entry for the **Post-Processing API Credential** rather than reusing an externally created entry.
- The first post-processing capability supports exactly one **Post-Processing Configuration**.
- The first **Post-Processing Configuration** uses **Anthropic-Compatible Post-Processing** only and rejects non-HTTPS endpoints.
- A **Post-Processing Configuration** cannot be enabled without its API credential; connectivity tests inform the user but do not automatically change enablement.
- The first implementation uses an application-owned **Post-Processing Policy** that users cannot directly edit.
- The first implementation exposes **Post-Processing Intensity**, defaults to conservative, and reserves aggressive behavior for later policy design.
- Saving the Dousha-owned **Post-Processing API Credential** enables **Dictation Post-Processing** by default, with a settings notice that transcript text is sent to the configured service.
- **Dictation Post-Processing** may be disabled without deleting its credential; deleting the credential disables it automatically.
- A failed **Post-Processing Unit** keeps its source text as the local assistant turn so later units retain complete session context and continue processing.
- Each request carries the complete accumulated Post-Processing Session history. The first version neither truncates nor summarizes it; provider context-limit rejection follows ordinary unit-failure fallback.
- Intermediate Post-Processing Units have no user-experience deadline, but every post-processing HTTP request has a 20-second transport timeout. A transport timeout preserves the source unit and advances the ordered queue.
- The terminal turn is the last unit request when reconciliation is unnecessary, or the Final Transcript Reconciliation request when reconciliation is required.
- The terminal turn's three-second insertion deadline begins at its actual send timestamp, including when it is sent before recording release. An expired deadline causes immediate insertion once the workflow reaches the terminal waiting state.
- After recording release, Dousha waits for complete final ASR before deciding the final tail and reconciliation requirement. Earlier queued requests remain bounded only by their individual transport timeouts.
- Waiting for complete final ASR reuses the Doubao transport's existing two-second finish timeout. Timeout is an unrecoverable workflow failure: partial ASR text is discarded, the clipboard is unchanged, and the next trigger is not guarded.
- Frozen Post-Processing Units enter a lossless ordered queue even when processing is slower than dictation.
- **Immediate Insertion** cancels the in-flight request, resolves all unfinished units to source text, preserves completed results, and discards late callbacks by session generation.
- A double-tap of Left Control triggers **Immediate Insertion** only while the Transcription Window Status is waiting for post-processing.
- The waiting-state **Immediate Insertion** gesture is a plain double-tap using the existing timing window; it does not require a hold and its key events are consumed.
- Escape triggers **Dictation Cancellation** only while recording or waiting for post-processing and is otherwise passed through.
- **Dictation Cancellation** clears the body, shows a cancelled title for approximately 1.5 seconds, and permits immediate replacement by a new recording.
- Conservative **Post-Processing Intensity** preserves a single short hesitation such as "嗯" or "呃" and collapses repetitions to one.
- Conservative **Post-Processing Intensity** removes explicit self-correction scaffolding while preserving the user's final intended statement.
- **Doubao Text Post-Processing** and **Dictation Post-Processing** are separate stages with different responsibilities.
- A thinking pause does not by itself close a **Post-Processing Unit**; the Windows design preserves incremental **Dictation Post-Processing** rather than depending on end-only **Doubao Text Post-Processing**.
- The Windows flow uses Doubao's no-pause-finalization and long-utterance guard parameters without enabling end-only **Doubao Text Post-Processing**.
- Rejection or pathological behavior from the no-pause-finalization guard set is an explicit dictation failure requiring diagnostics, not a trigger for legacy-config fallback.
- An unrecoverable workflow failure does not activate Paste Failure Recovery and does not block the next Double-Tap-and-Hold Trigger.
- Dictation failure or clipboard-write failure displays `听写失败，请查看日志` for approximately 1.5 seconds, then fades out unless a new recording replaces it sooner.
- The **First Usable Windows Version** uses **Default Microphone Capture**.
- The **First Usable Windows Version** is delivered as a **Portable Windows Release**.
- A **Portable Windows Release** may use a **Startup Shortcut** for launch at login.
- The **Windows Port** is a **Personal Utility**.
- The **First Usable Windows Version** uses **Mandarin-First Dictation**.
- **Mixed-Language Dictation** is a required future capability after the **First Usable Windows Version**.
- **Dictation Post-Processing** is a future capability after the **First Usable Windows Version**.
- Each recording that enables **Dictation Post-Processing** owns exactly one **Post-Processing Session**.
- A **Post-Processing Session** receives ordered **Post-Processing Units**, including punctuation-free trailing phrases submitted when recording ends.
- During recording, **Post-Processing Units** are frozen from the **Stable Transcript Prefix**, not from pause-driven ASR finalization.
- A boundary enters the **Stable Transcript Prefix** after at least two recognition updates, 500 milliseconds of stability, and newer text beyond the boundary.
- Once frozen, a **Post-Processing Unit** cannot be revised by later recognition updates.
- When Dictation Post-Processing is disabled, no append-only conversation exists and the complete final ASR result replaces the visual Transcript Buffer before insertion.
- A conflict between the final ASR result and frozen units triggers **Final Transcript Reconciliation** in the same Post-Processing Session.
- Successful **Final Transcript Reconciliation** replaces the complete Transcript Buffer and marks it entirely post-processed.
- **Final Transcript Reconciliation** is skipped when the final ASR result remains compatible with the frozen source prefix.
- Final conflict comparison ignores whitespace and equivalent full-width or half-width punctuation but treats lexical, numeric, and other character changes as substantive.
- **Glossary Context** may later supply shared terminology to transcription and post-processing, but glossary maintenance remains a separate future capability.
- **Immediate Insertion** keeps completed post-processing results, uses source text for unfinished units, and cancels the remaining **Post-Processing Session**.
- When the terminal post-processing deadline expires, insertion uses the best available text and the request may continue within its transport timeout.
- A late completion may replace the clipboard only while the timeout-insertion clipboard sequence is unchanged and no newer Dousha recording has written the clipboard.
- Operational paste success means the transcript was written to the clipboard and every required paste input event was dispatched. It does not assert that the target application accepted or rendered the text.
- Paste Failure Recovery is entered only when the transcript was written to the clipboard but paste input dispatch failed. Clipboard-write failure uses the transient unrecoverable workflow-failure state because no recovery clipboard exists.
- **Clipboard Paste Insertion** targets the active text target at insertion time; it does not restore the application focused when recording began.
- **Glossary Context** is an early future capability after the **First Usable Windows Version**.
- The **First Usable Windows Version** uses a **Double-Tap-and-Hold Trigger** by default.
- The **Core Dictation Experience** uses **Clipboard Paste Insertion** for text insertion.
- The **First Usable Windows Version** writes a **Local Diagnostic Log**.
- The **First Usable Windows Version** uses **Non-Blocking Error Feedback**.
- The **First Usable Windows Version** uses **Normal-Privilege Operation**.
- **Multi-Engine Routing** is outside the **First Usable Windows Version**.

## Example Dialogue

> **Dev:** "Does the Windows Port need the same menu-bar behavior as macOS?"
> **Domain expert:** "No. It needs the Core Dictation Experience, surfaced as a Windows Tray Dictation App."
>
> **Dev:** "How should DoushaWin paste into a WSL desktop window?"
> **Domain expert:** "Detect WSL Desktop Paste Targets by their window-title prefix at injection time, and send the configured Xwayland Paste Shortcut instead of the standard paste shortcut."
>
> **Dev:** "Should it detect Xwayland by process name or window class?"
> **Domain expert:** "Use Foreground Paste Detection on the window title; the user can configure the prefix if their WSL setup uses a different title."
>
> **Dev:** "What if the active window title cannot be read due to privilege isolation?"
> **Domain expert:** "Fall back to the standard paste shortcut; never block insertion because the detection failed."
>
> **Dev:** "Should the first Windows release include all engines and routing?"
> **Domain expert:** "No. The First Usable Windows Version should prove the Single-Engine Dictation Path first."
>
> **Dev:** "Can the first Windows version use another cloud ASR before Doubao is ready?"
> **Domain expert:** "No. Without the Doubao Dictation Path, the Windows Port loses the product identity."
>
> **Dev:** "Can the first Windows version ask users to paste Doubao tokens manually?"
> **Domain expert:** "No. It should manage Doubao Device Credentials automatically."
>
> **Dev:** "Should the MVP encrypt the Doubao credential cache?"
> **Domain expert:** "No. The First Usable Windows Version uses a Plain Credential Cache."
>
> **Dev:** "Can the post-processing API key use the Plain Credential Cache?"
> **Domain expert:** "No. A user-provided Post-Processing API Credential belongs in Windows Credential Manager."
>
> **Dev:** "Should Dousha reuse an existing personal Credential Manager entry?"
> **Domain expert:** "No. Dousha creates and owns its own dedicated credential entry."
>
> **Dev:** "Can the user maintain multiple post-processing provider profiles?"
> **Domain expert:** "Not initially. There is one active Post-Processing Configuration."
>
> **Dev:** "Does the first version also need an OpenAI-compatible adapter?"
> **Domain expert:** "No. It uses the Anthropic-compatible Messages API with thinking disabled."
>
> **Dev:** "Can a configured post-processing endpoint use plain HTTP?"
> **Domain expert:** "No. The first Post-Processing Configuration accepts HTTPS endpoints only."
>
> **Dev:** "Should a failed connection test automatically disable post-processing?"
> **Domain expert:** "No. Only a missing credential prevents enablement; runtime failures silently preserve source text."
>
> **Dev:** "Can the user edit the post-processing system prompt?"
> **Domain expert:** "Not initially. The application owns the Post-Processing Policy and its conversation protocol."
>
> **Dev:** "Does the user need to enable post-processing separately after saving a credential?"
> **Domain expert:** "No. Saving the Dousha-owned credential enables it by default and the settings UI discloses transcript transmission."
>
> **Dev:** "Must the user delete the credential to stop post-processing?"
> **Domain expert:** "No. Enablement is independently reversible, while credential deletion also disables it."
>
> **Dev:** "Does one failed unit invalidate the Post-Processing Session?"
> **Domain expert:** "No. Record the source text as that unit's local assistant result and continue with later units."
>
> **Dev:** "Does every Post-Processing Unit have a three-second timeout?"
> **Domain expert:** "No. Every request has a 20-second transport timeout, while only the terminal turn has the three-second insertion deadline measured from its actual send time."
>
> **Dev:** "Can release of the recording reset the terminal deadline?"
> **Domain expert:** "No. If the terminal turn was already sent, its original send timestamp remains authoritative."
>
> **Dev:** "Should queued units be merged or dropped when the model falls behind?"
> **Domain expert:** "No. Stable units remain in a lossless ordered queue and retain regular text weight until processed."
>
> **Dev:** "Can a cancelled post-processing response update the clipboard later?"
> **Domain expert:** "No. Immediate Insertion invalidates the session generation and discards every late callback."
>
> **Dev:** "Does a double-tap always mean Immediate Insertion?"
> **Domain expert:** "No. It means Immediate Insertion only while waiting for post-processing; other states keep their defined trigger behavior."
>
> **Dev:** "Must the user hold the second press to request Immediate Insertion?"
> **Domain expert:** "No. A plain consumed double-tap is enough while waiting."
>
> **Dev:** "How does the user discard an active dictation?"
> **Domain expert:** "Press Escape while recording or waiting; Dousha consumes it and discards the entire session."
>
> **Dev:** "How long is cancellation feedback visible?"
> **Domain expert:** "The cleared window shows a cancelled title for approximately 1.5 seconds before fading."
>
> **Dev:** "Should conservative post-processing remove phrases such as '就是说'?"
> **Domain expert:** "No. Conservative intensity preserves discourse markers unless they are clearly accidental repetition or recognition noise."
>
> **Dev:** "What should happen to repeated short hesitations?"
> **Domain expert:** "Keep one occurrence of '嗯' or '呃' and remove only the repetition."
>
> **Dev:** "Should conservative post-processing preserve '周三，呃不，周四' verbatim?"
> **Domain expert:** "No. It should preserve the corrected meaning, '周四', and remove the superseded expression."
>
> **Dev:** "Is Doubao's text post-process flag the same as Dictation Post-Processing?"
> **Domain expert:** "No. Doubao Text Post-Processing controls ASR finalization and punctuation; Dictation Post-Processing handles terminology and spoken-expression noise."
>
> **Dev:** "Should a thinking pause finalize a Post-Processing Unit?"
> **Domain expert:** "No. A unit closes at a textual boundary or recording completion, not merely because the speaker paused."
>
> **Dev:** "Should the Windows flow enable Doubao's end-only text post-process?"
> **Domain expert:** "No. It uses the no-pause-finalization guard set while preserving incremental Dictation Post-Processing."
>
> **Dev:** "Should an invalid guard-set session retry with the old Doubao configuration?"
> **Domain expert:** "No. Fail explicitly, record diagnostics, and surface Non-Blocking Error Feedback for debugging."
>
> **Dev:** "Should a dictation failure block the next trigger?"
> **Domain expert:** "No. There is no clipboard result to protect, so the next trigger begins a new recording."
>
> **Dev:** "What if Doubao never returns the complete final result?"
> **Domain expert:** "The existing two-second finish timeout ends the wait as an unrecoverable workflow failure and discards partial ASR text."
>
> **Dev:** "Does a dictation failure remain collapsed on screen?"
> **Domain expert:** "No. It fades out after approximately 1.5 seconds and may be replaced immediately by a new recording."
>
> **Dev:** "Should the MVP include a microphone picker?"
> **Domain expert:** "No. It uses Default Microphone Capture."
>
> **Dev:** "Does the MVP need a Windows installer?"
> **Domain expert:** "No. It should be a Portable Windows Release and may use a Startup Shortcut."
>
> **Dev:** "Does the MVP need automatic updates?"
> **Domain expert:** "No. As a Personal Utility, it uses manual updates."
>
> **Dev:** "Should holding Left Control once start dictation?"
> **Domain expert:** "No. Use the Double-Tap-and-Hold Trigger so normal Control shortcuts are not captured."
>
> **Dev:** "Should we restore the user's previous clipboard after insertion?"
> **Domain expert:** "No. Clipboard Paste Insertion keeps the dictated text on the clipboard to avoid paste timing races."
>
> **Dev:** "Should insertion return to the application that was focused when recording began?"
> **Domain expert:** "No. It uses the active text target at insertion time; without one, the complete text remains on the clipboard with Non-Blocking Error Feedback."
>
> **Dev:** "Does the Windows MVP need the macOS floating HUD?"
> **Domain expert:** "No. It only needs Minimal Status Feedback."
>
> **Dev:** "Can the user edit text directly in the Transcription Window?"
> **Domain expert:** "Not initially. It remains non-activating so the active text target keeps keyboard focus."
>
> **Dev:** "How does the Transcription Window show transcript progress?"
> **Domain expert:** "Interim text is dimmed, frozen source text waiting for post-processing uses regular weight, and post-processed text replaces it in bold."
>
> **Dev:** "Is unchanged text still bold after successful post-processing?"
> **Domain expert:** "Yes. Bold indicates completion, not textual difference."
>
> **Dev:** "Should a failed Post-Processing Unit show an additional warning?"
> **Domain expert:** "No. It silently remains as regular-weight frozen source text."
>
> **Dev:** "Does disabling Dictation Post-Processing also disable the Transcription Window?"
> **Domain expert:** "No. Live transcription visualization remains available independently."
>
> **Dev:** "Can the user disable the Transcription Window?"
> **Domain expert:** "No. It is the standard interaction surface for dictation progress and recovery."
>
> **Dev:** "Where should recording-level progress be shown?"
> **Domain expert:** "The title bar shows dictating, waiting for final transcription, waiting for post-processing, pasting, success, failure, or cancellation."
>
> **Dev:** "Does '粘贴成功' prove that the target application inserted the text?"
> **Domain expert:** "No. It means Dousha wrote the clipboard and dispatched every required paste input event successfully; target acceptance is not portably observable."
>
> **Dev:** "What if Dousha cannot write the transcript to the clipboard?"
> **Domain expert:** "Do not enter Paste Failure Recovery because there is nothing to protect. Show transient unrecoverable failure, log metadata, and allow the next recording trigger."
>
> **Dev:** "Should the title bar also explain active shortcuts?"
> **Domain expert:** "No. Shortcut hints make the compact title layout too long."
>
> **Dev:** "Must the first status labels follow the Windows display language?"
> **Domain expert:** "No. The first implementation uses fixed Chinese labels."
>
> **Dev:** "Can the next trigger immediately start recording after paste failure?"
> **Domain expert:** "No. Paste Failure Recovery protects Dousha's clipboard result; the next double-tap only acknowledges the failure."
>
> **Dev:** "How should a long transcript affect the window?"
> **Domain expert:** "The Transcription Window grows upward to five body lines, then scrolls while keeping its title and latest text visible."
>
> **Dev:** "Should the Transcription Window follow focus across displays during recording?"
> **Domain expert:** "No. It selects the foreground application's display at recording start and stays there for that recording."
>
> **Dev:** "Should language selection be part of the first Windows settings window?"
> **Domain expert:** "No. Start with Mandarin-First Dictation, while keeping Mixed-Language Dictation as a future capability."
>
> **Dev:** "Does the Windows MVP need Dictation Post-Processing?"
> **Domain expert:** "No. Dictation Post-Processing is a future capability."
>
> **Dev:** "Should each unit be post-processed without the preceding turns?"
> **Domain expert:** "No. One recording uses one ordered Post-Processing Session so earlier source units and results remain conversational context."
>
> **Dev:** "What happens when a short dictation has no punctuation?"
> **Domain expert:** "Recording completion submits the remaining phrase as a Post-Processing Unit."
>
> **Dev:** "How can post-processing start when pause-driven finalization is disabled?"
> **Domain expert:** "Textual boundaries are frozen only after they enter the Stable Transcript Prefix."
>
> **Dev:** "Does a punctuation mark freeze immediately when first recognized?"
> **Domain expert:** "No. It must persist across two updates for 500 milliseconds and have newer text after it."
>
> **Dev:** "Can a later recognition update rewrite a frozen unit?"
> **Domain expert:** "No. Later recognition changes apply only to the unfrozen tail."
>
> **Dev:** "What if post-processing is disabled and the final ASR result changes earlier text?"
> **Domain expert:** "Without model conversation history, the complete final ASR result replaces the visual buffer before insertion."
>
> **Dev:** "How is a final ASR result reconciled when it conflicts with frozen units?"
> **Domain expert:** "The same Post-Processing Session receives the final ASR text and assembled result and returns one complete reconciled transcript."
>
> **Dev:** "Should every recording receive a final whole-transcript post-processing turn?"
> **Domain expert:** "No. Final Transcript Reconciliation runs only when the final ASR result conflicts with frozen source text."
>
> **Dev:** "Does punctuation width alone trigger Final Transcript Reconciliation?"
> **Domain expert:** "No. Whitespace and equivalent punctuation are normalized before conflict comparison."
>
> **Dev:** "Does this capability include a glossary maintenance experience?"
> **Domain expert:** "No. It only preserves an empty glossary input boundary for future transcription and post-processing use."
>
> **Dev:** "Should a late model result update the clipboard after the user requests Immediate Insertion?"
> **Domain expert:** "No. Immediate Insertion explicitly abandons unfinished post-processing; only a system timeout may produce a deferred clipboard update."
>
> **Dev:** "Does the Windows MVP need a glossary?"
> **Domain expert:** "No. Glossary Context is an early future capability after the first Doubao path works."
>
> **Dev:** "Should the Windows MVP collect telemetry?"
> **Domain expert:** "No. It only writes a Local Diagnostic Log for local troubleshooting."
>
> **Dev:** "Should dictation errors show blocking dialogs?"
> **Domain expert:** "No. Use Non-Blocking Error Feedback and write details to the Local Diagnostic Log."
>
> **Dev:** "Should the app run elevated so it can paste into administrator windows?"
> **Domain expert:** "No. The First Usable Windows Version uses Normal-Privilege Operation and does not target elevated windows."
>
> **Dev:** "Should Multi-Engine Routing be included if the macOS app already has it?"
> **Domain expert:** "No. It is outside the First Usable Windows Version."

## Flagged Ambiguities

- "Xwayland detection" was resolved as **Foreground Paste Detection** using a configurable window-title prefix, not process-name or window-class matching.
- "Xwayland paste key" was resolved as the configurable **Xwayland Paste Shortcut**, defaulting to `alt+v`.
- "When to detect the Xwayland target" was resolved as at injection time, not at recording start.
- "Window-title read failure" was resolved as a silent fallback to the standard paste shortcut.
- "Multi-engine parallel routing" was resolved as outside the First Usable Windows Version.
- "Generic cloud ASR MVP" was rejected; the First Usable Windows Version must include the Doubao Dictation Path.
- "Manual Doubao token entry" was rejected; the First Usable Windows Version should manage Doubao Device Credentials automatically.
- "Encrypted credential storage" was deferred; the First Usable Windows Version uses a Plain Credential Cache.
- The Plain Credential Cache applies only to automatically managed Doubao Device Credentials; user-provided post-processing API keys require Windows Credential Manager.
- The **Post-Processing API Credential** uses a Dousha-owned Credential Manager target rather than an external credential name.
- Multiple post-processing provider profiles and quick switching remain out of scope.
- OpenAI-compatible post-processing remains out of scope for the first implementation.
- Non-HTTPS post-processing endpoints are rejected rather than treated as an advanced configuration.
- Connection testing is advisory, while missing credentials are the only configuration-level enablement block.
- Free-form system-prompt editing remains out of scope for the first implementation.
- A separate first-use enable confirmation was rejected; credential setup itself enables post-processing by default.
- Post-processing enablement and credential presence are distinct persisted states.
- Unit-level post-processing failure does not circuit-break the recording's session.
- Per-unit user-experience deadlines during recording were rejected because late turns would fork the ordered conversation history; a 20-second transport timeout still bounds each HTTP request.
- Backpressure does not alter recognition freezing or discard Post-Processing Units.
- Late post-processing callbacks after Immediate Insertion cannot update the Transcript Buffer or clipboard.
- Immediate Insertion is state-scoped and cannot duplicate paste or interrupt active recording semantics.
- Waiting-state Immediate Insertion reuses trigger timing but not the hold requirement.
- Dictation Cancellation never inserts text or updates the clipboard.
- Cancellation feedback is transient and does not guard the next trigger.
- Aggressive **Post-Processing Intensity** is represented as a future option, but its transformation policy remains out of scope.
- Conservative intensity collapses repeated short hesitation tokens rather than deleting them entirely.
- Explicit self-correction traces are semantic noise in conservative mode and should collapse to the final corrected expression.
- "Doubao post-process" was disambiguated as **Doubao Text Post-Processing**, not the language-model **Dictation Post-Processing** capability.
- End-only **Doubao Text Post-Processing** was rejected for this Windows flow because it would prevent incremental post-processing during recording.
- The three-parameter no-pause-finalization guard set requires Windows real-device validation because the research PR validated it only as part of a five-parameter combination.
- Legacy Doubao configuration fallback was rejected because a guard-set failure makes the intended flow invalid and should remain visible for debugging.
- Only paste failure guards the next trigger; dictation failure permits immediate retry.
- "Paste failure" means clipboard write succeeded but paste dispatch failed. Clipboard-write failure is an unguarded unrecoverable workflow failure.
- Dictation-failure feedback is transient rather than a persistent recovery state.
- "Microphone selection" was deferred; the First Usable Windows Version uses Default Microphone Capture.
- "Installer-first release" was rejected for the MVP; the First Usable Windows Version is a Portable Windows Release.
- "Automatic updates" were rejected because the Windows Port is a Personal Utility.
- "Left Control" was resolved to mean a Double-Tap-and-Hold Trigger by default, not single-press recording.
- "Polite clipboard restore" was rejected for Clipboard Paste Insertion because it can race the target application's paste handling.
- "English and mixed input" was resolved as required future capability, while the First Usable Windows Version starts with Mandarin-First Dictation.
- "LLM polish" and "cleanup" were replaced by **Dictation Post-Processing**: terminology correction, terminology consistency, and dictated-filler removal without stylistic rewriting.
- The post-processing conversation was resolved to one **Post-Processing Session** per recording, never shared across recordings.
- "Sentence" was replaced by **Post-Processing Unit** because a submitted fragment may be a complete sentence, a soft comma split, or a punctuation-free trailing phrase.
- Pause-driven ASR finalization was replaced by the **Stable Transcript Prefix** as the incremental freezing signal.
- The first stable-prefix policy uses two updates, 500 milliseconds, and trailing recognized text.
- Frozen units are append-only conversation history and cannot be rolled back.
- Character-level guessing was rejected for frozen-prefix conflicts; use conditional **Final Transcript Reconciliation** instead.
- Routine whole-transcript reconciliation was rejected because it adds latency and token usage without resolving a conflict.
- Final conflict detection ignores formatting-equivalent differences and triggers only for substantive frozen-source changes.
- Glossary maintenance UX remains out of scope; only a future-compatible empty **Glossary Context** boundary is retained.
- A system timeout may preserve a late result for the clipboard, while **Immediate Insertion** cancels and discards unfinished results.
- A deferred clipboard update contains the complete final transcript, never only the unit that completed late.
- A deferred clipboard update requires unchanged clipboard-sequence and recording-generation ownership.
- Paste success is an operational integration result, not confirmation that an arbitrary target application rendered the text.
- The **Transcription Window** was resolved as a non-activating visualization of the transcript buffer, not an editable text window.
- Transcript state styling was resolved as dimmed interim text, regular frozen source text, and bold post-processed text.
- Bold text represents successful post-processing independently of whether the content changed.
- Sentence-level post-processing failures use no additional visual or blocking feedback.
- The **Transcription Window** and **Dictation Post-Processing** were resolved as independent capabilities.
- A Transcription Window enable or disable preference was rejected because the window is part of the core interaction.
- Recording-level progress belongs in the **Transcription Window Status**, while unit-level progress remains encoded by text weight and color.
- Shortcut discoverability belongs outside the Transcription Window title bar.
- Transcription Window localization remains out of scope for the first implementation.
- Paste failure was resolved as a guarded recovery state with delayed title-only collapse and explicit trigger acknowledgement.
- The **Transcription Window Layout** was resolved as fixed-width, five-line-capped, bottom-centered, and flush with the taskbar work area.
- Multi-display placement is selected once per recording from the foreground application, with pointer display as fallback.
- The insertion target was resolved as the active text target at insertion time, not a captured recording-start target.
- "Glossary/context hints" were resolved as an early future capability, not part of the First Usable Windows Version.
- "Windows 移植版" was resolved to mean a Windows-native edition that preserves the core dictation experience, not a direct Swift/macOS UI clone.
