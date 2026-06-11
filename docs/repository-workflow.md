# Repository Workflow

## Remotes and long-lived branches

- `origin` points to `fenixfu/dousha`.
- `upstream` points to `giraphant/dousha`.
- `main` is a fast-forward-only mirror of `upstream/main`.
- `develop` is the fork's long-lived integration branch and is never rebased or force-pushed.

The primary checkout remains on `develop`. A persistent sibling worktree at `../dousha-upstream` checks out `main`.

## Branch families

- `feature/<topic>` starts from `develop` and carries production work for this fork.
- `sync/upstream-YYYYMMDD` starts from `develop`, merges the current mirrored `main`, and returns through a pull request to `develop`.
- `eval/postprocess` defines shared evaluation inputs, timing points, result formats, and scoring rules.
- `eval/postprocess-doubao`, `eval/postprocess-llm`, and `eval/postprocess-hybrid` are short-lived strategy evaluations derived from `eval/postprocess`.
- `contrib/<topic>` starts from the latest `upstream/main` and contains only changes suitable for a pull request to `giraphant/dousha`.

Temporary sync, evaluation, feature, and contribution branches use separate sibling worktrees. Remove each temporary worktree and its local branch after its pull request or evaluation is complete.

## Pull upstream changes

Update the persistent upstream worktree:

```powershell
git fetch upstream --prune
git -C ..\dousha-upstream merge --ff-only upstream/main
git -C ..\dousha-upstream push origin main
```

Never commit directly to `main`.

## Integrate upstream into develop

Create a dated integration worktree:

```powershell
git worktree add -b sync/upstream-YYYYMMDD ..\dousha-sync-YYYYMMDD develop
git -C ..\dousha-sync-YYYYMMDD merge --no-ff main
```

Resolve conflicts using the ownership rules below, test both implementations, push the branch, and open a pull request targeting `develop`. Do not rebase `develop`.

## Path ownership

Upstream owns:

- Swift source and tests
- `Package.swift`
- macOS resources and release machinery
- upstream CI
- `ARCHITECTURE.md`

The fork owns:

- `windows/`
- `.agents/`
- `ORCHESTRATOR.md`, `EXECUTOR.md`, `TESTER.md`, `REVIEWER.md`, and `BUILDER.md`
- `docs/agents/`
- evaluation documents and results

Review root coordination files manually. Upstream `AGENTS.md` project instructions map into this fork's `PROJECT.md`; this fork's `AGENTS.md` remains the role router and collaboration policy. Never resolve this mapping with a blanket `ours` or `theirs`.

## Upstream sync checklist

Every `sync/*` pull request must confirm:

- Upstream `AGENTS.md` changes were reviewed and relevant project instructions were ported to `PROJECT.md`.
- Swift source, tests, package layout, resources, and upstream CI match the intended upstream revision.
- `windows/`, `.agents/`, role instructions, and fork-specific documentation remain intact.
- Root coordination files were integrated manually.
- Swift tests and .NET tests were run, or any unavailable platform testing is stated explicitly.
- The pull request records the exact upstream commit being integrated.

## Contribute to upstream

Create contribution branches directly from the latest upstream revision:

```powershell
git fetch upstream --prune
git worktree add -b contrib/<topic> ..\dousha-contrib-<topic> upstream/main
```

Commit only independently useful Swift/upstream changes, then push the branch to the fork and open a pull request from `fenixfu:contrib/<topic>` to `giraphant:main`.

Do not cherry-pick fork integration commits from `develop` into a contribution branch. After the upstream pull request merges, retrieve the change through the normal `main` mirror and `sync/*` process.

## Post-processing evaluation

The shared `eval/postprocess` baseline fixes evaluation inputs and measurement rules for:

- Doubao `last_post_process`
- incremental LLM Dictation Post-Processing
- Doubao `last_post_process` followed by LLM Dictation Post-Processing

Evaluation branches may use Swift or .NET, but must use the shared inputs, timing points, result format, and scoring rules. Lock the upstream baseline for an evaluation cycle. Only a specifically relevant upstream fix should trigger a new sync during that cycle.

After evaluation, tag or otherwise preserve the results, remove the strategy branches, and implement only the selected strategy on a new `feature/*` branch from `develop`.

## Local reference clones

Do not keep nested Git repositories such as `docs/research_repos/dousha/` as synchronization sources. Use the `upstream` remote and worktrees so all comparisons refer to one object database and one recorded history.
