# Concentus for Windows Doubao Opus Proof

Issue #9 uses `Concentus` 2.2.2 as the Windows Opus encoding path for the Doubao Dictation Path proof slice.

`Concentus` is a pure .NET Opus encoder/decoder package, which avoids native `libopus` packaging risk in the Portable Windows Release. That tradeoff matters for the Personal Utility MVP because publish output should stay simple and runnable without an installer or architecture-specific native dependency checks.

The alternative researched path was `OpusSharp` / `OpusSharp.Core`, which is more recently updated but wraps native Opus libraries. That may still be a future option if real Doubao acceptance shows quality, bitrate, or compatibility problems with `Concentus`; issue #9 deliberately proves the managed path first.

This ADR does not add generic ASR fallback, Soniox-first routing, Multi-Engine Routing, or the full dictation loop. It only records the selected encoding dependency and the architectural consequence that Windows Doubao audio transport can begin as a managed, portable proof adapter.
