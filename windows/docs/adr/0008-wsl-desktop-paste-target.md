---
status: accepted
---

# WSL Desktop Paste Target

The owner runs a full Xfce desktop environment inside WSL rather than using WSLg directly. In that setup the Windows and Linux clipboards are not shared, so the standard `Ctrl+V` paste does nothing for Linux GUI applications hosted on Xwayland. The owner therefore maintains a small Xfce-side patch that reads the Windows clipboard and performs the paste, bound to `Alt+V`.

DoushaWin detects these windows at injection time by a configurable title prefix (`Xwayland on :` by default) and sends a configurable shortcut (`alt+v` by default) instead of the normal `Ctrl+V`. Both the prefix and the shortcut are user-configurable because the exact Xwayland window title and the local patch's trigger key are tied to the owner's WSL desktop setup.

We chose window-title prefix matching rather than process-name or window-class detection because the observed, stable signal is the title (`Xwayland on :0 (Ubuntu)`), while the underlying process names vary with the WSL distribution and Xfce launcher configuration. If the title cannot be read due to UIPI or an elevated target, DoushaWin silently falls back to the standard paste shortcut rather than blocking insertion.
