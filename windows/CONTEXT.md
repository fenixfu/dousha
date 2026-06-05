# Windows Port

The Windows Port context defines the product language for Dousha's Windows edition and separates the user-facing dictation experience from platform-specific integrations.

## Language

**Windows Port**:
The Windows edition of Dousha that preserves the core dictation experience while using Windows-native integrations.
_Avoid_: Direct Swift port, macOS clone

**Core Dictation Experience**:
A flow where the user triggers recording, speaks, receives a transcript, and the resulting text appears in the active text target.
_Avoid_: Feature parity, full clone

**Windows Tray Dictation App**:
A background Windows app surfaced from the notification area for settings and status.
_Avoid_: Menu-bar app, Dock app

**Settings Window**:
A lightweight Windows window where users configure dictation behavior and credentials.
_Avoid_: Preferences pane, settings page

**Minimal Status Feedback**:
Tray-level recording, transcribing, success, and error feedback without a full floating dictation HUD.
_Avoid_: Full HUD, waveform overlay, live transcript window

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

**LLM Refinement**:
A post-transcription step that rewrites or polishes dictated text before or after insertion.
_Avoid_: Core transcription, ASR engine

**Glossary Context**:
User-provided terms sent as recognition context to improve transcription of domain words and names.
_Avoid_: LLM refinement, language routing

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
- The **Windows Port** is delivered as a **Windows Tray Dictation App**.
- A **Windows Tray Dictation App** exposes configuration through a **Settings Window**.
- A **Windows Tray Dictation App** provides **Minimal Status Feedback** in the **First Usable Windows Version**.
- The **First Usable Windows Version** uses the **Doubao Dictation Path**.
- The **Doubao Dictation Path** is a **Single-Engine Dictation Path**.
- The **Doubao Dictation Path** requires **Doubao Device Credentials**.
- The **First Usable Windows Version** stores **Doubao Device Credentials** in a **Plain Credential Cache**.
- The **First Usable Windows Version** uses **Default Microphone Capture**.
- The **First Usable Windows Version** is delivered as a **Portable Windows Release**.
- A **Portable Windows Release** may use a **Startup Shortcut** for launch at login.
- The **Windows Port** is a **Personal Utility**.
- The **First Usable Windows Version** uses **Mandarin-First Dictation**.
- **Mixed-Language Dictation** is a required future capability after the **First Usable Windows Version**.
- **LLM Refinement** is a future capability after the **First Usable Windows Version**.
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
> **Dev:** "Does the Windows MVP need the macOS floating HUD?"
> **Domain expert:** "No. It only needs Minimal Status Feedback."
>
> **Dev:** "Should language selection be part of the first Windows settings window?"
> **Domain expert:** "No. Start with Mandarin-First Dictation, while keeping Mixed-Language Dictation as a future capability."
>
> **Dev:** "Does the Windows MVP need LLM polish?"
> **Domain expert:** "No. LLM Refinement is a future capability."
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

- "Multi-engine parallel routing" was resolved as outside the First Usable Windows Version.
- "Generic cloud ASR MVP" was rejected; the First Usable Windows Version must include the Doubao Dictation Path.
- "Manual Doubao token entry" was rejected; the First Usable Windows Version should manage Doubao Device Credentials automatically.
- "Encrypted credential storage" was deferred; the First Usable Windows Version uses a Plain Credential Cache.
- "Microphone selection" was deferred; the First Usable Windows Version uses Default Microphone Capture.
- "Installer-first release" was rejected for the MVP; the First Usable Windows Version is a Portable Windows Release.
- "Automatic updates" were rejected because the Windows Port is a Personal Utility.
- "Left Control" was resolved to mean a Double-Tap-and-Hold Trigger by default, not single-press recording.
- "Polite clipboard restore" was rejected for Clipboard Paste Insertion because it can race the target application's paste handling.
- "English and mixed input" was resolved as required future capability, while the First Usable Windows Version starts with Mandarin-First Dictation.
- "LLM polish" was resolved as a future capability, not part of the First Usable Windows Version.
- "Glossary/context hints" were resolved as an early future capability, not part of the First Usable Windows Version.
- "Windows 移植版" was resolved to mean a Windows-native edition that preserves the core dictation experience, not a direct Swift/macOS UI clone.
