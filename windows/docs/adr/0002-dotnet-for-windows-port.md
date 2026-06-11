---
status: superseded by ADR-0007
---

# .NET for the Windows port

The Windows port will use C#/.NET as its main implementation stack. Dousha's Windows MVP depends on Windows-native desktop integration points such as tray presence, global hotkeys, microphone capture, clipboard access, synthetic paste input, startup registration, and installer packaging; .NET gives the most direct path to those APIs while keeping the Doubao transport and state machine maintainable.
