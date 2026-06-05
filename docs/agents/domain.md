# Domain Docs

How the engineering skills should consume this repo's domain documentation when exploring the codebase.

## Layout

This repository uses a multi-context layout because it is a fork of a macOS app and the project direction includes a Windows port.

- Root-level docs describe cross-platform concepts and decisions.
- The existing macOS app context should stay separate from Windows-specific porting context.
- Windows-specific concepts, packaging choices, OS integrations, and ADRs should live in a Windows context once that area is created.

## Before exploring, read these

- `CONTEXT-MAP.md` at the repo root, if it exists. It should point to the relevant context docs.
- The context-specific `CONTEXT.md` for the area being changed.
- `docs/adr/` for cross-platform decisions.
- Context-scoped ADRs, such as `windows/docs/adr/`, when working in that context.

If any of these files do not exist yet, proceed silently. Do not flag their absence or create them unless the current task needs domain language or architectural decisions to be recorded.

## Expected future shape

```text
/
|-- CONTEXT-MAP.md
|-- docs/
|   |-- adr/
|   `-- agents/
|-- Sources/
|-- Tests/
`-- windows/
    |-- CONTEXT.md
    `-- docs/
        `-- adr/
```

The exact Windows directory name can change when the port is scaffolded. If it changes, update `CONTEXT-MAP.md` and this file so agents can find the right context.

## Use the glossary's vocabulary

When output names a domain concept in an issue title, refactor proposal, hypothesis, or test name, use the term as defined in the relevant `CONTEXT.md`.

If the concept is missing from the glossary, either reconsider whether the project already has a better term or note the gap for `grill-with-docs`.

## Flag ADR conflicts

If output contradicts an existing ADR, surface it explicitly rather than silently overriding it.
