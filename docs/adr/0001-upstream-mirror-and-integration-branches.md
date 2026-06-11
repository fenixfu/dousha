---
status: accepted
---

# Separate upstream mirroring from local integration

The repository uses `upstream` for `giraphant/dousha` and `origin` for `fenixfu/dousha`. Local `main` remains a fast-forward-only mirror of `upstream/main`; local work integrates on `develop`, feature and experiment branches start from `develop`, and contribution branches start from `upstream/main` so they can be proposed back upstream without carrying the .NET Windows Port or local project history.

Upstream changes enter `develop` through a dated `sync/upstream-YYYYMMDD` branch and a reviewed pull request. The sync branch merges the mirrored `main` with an explicit merge commit; `develop` is not rebased or force-pushed.

The primary checkout remains on `develop`. A persistent sibling worktree checks out `main` for upstream inspection and fast-forward synchronization; dated sync, named experiment, and named contribution worktrees are temporary and are removed with their local branches after their pull requests or evaluations finish.

Changes intended for `giraphant/dousha` start on `contrib/<topic>` from the latest `upstream/main`, are pushed to the fork, and are proposed directly to `giraphant/main`. After upstream accepts them, they return to `develop` only through the ordinary upstream mirror and sync flow; local integration commits are not cherry-picked into contribution branches.

Post-processing comparisons use a shared `eval/postprocess` baseline for evaluation inputs, timing points, measurements, result formats, and scoring rules. Short-lived `eval/postprocess-doubao`, `eval/postprocess-llm`, and `eval/postprocess-hybrid` branches derive from that baseline in separate worktrees and may use Swift or .NET as appropriate. After evaluation, only the selected strategy is rebuilt as a formal `feature/*` branch for integration into `develop`.

Upstream synchronization is manual. A new evaluation cycle begins from a reviewed upstream sync, and its baseline remains fixed during that cycle unless a specifically relevant upstream fix justifies starting another sync; no bot automatically merges upstream changes into `develop`.

Path ownership guides conflict resolution. Upstream owns the Swift source tree, `Package.swift`, macOS resources, upstream CI, and `ARCHITECTURE.md`; the fork owns `windows/`, `.agents/`, role instruction files, `docs/agents/`, and evaluation material. Root coordination files are integrated manually. In particular, upstream `AGENTS.md` project instructions map semantically into the fork's `PROJECT.md`, while the fork's `AGENTS.md` remains the role router and collaboration policy; synchronization must review and port upstream instruction changes rather than resolving that path with a blanket `ours` or `theirs`.

The nested reference clone under `docs/research_repos/dousha/` is not a synchronization mechanism and should not be committed. Upstream inspection and synchronization use the `upstream` remote and dedicated worktrees instead.
