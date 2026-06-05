# Issue tracker: GitHub

Issues and PRDs for this repo live as GitHub issues for `fenixfu/dousha`. Use the `gh` CLI for issue operations when working locally, or the GitHub connector when available in the current session.

## Conventions

- Create an issue: `gh issue create --title "..." --body "..."`
- Read an issue: `gh issue view <number> --comments`
- List issues: `gh issue list --state open --json number,title,body,labels,comments`
- Comment on an issue: `gh issue comment <number> --body "..."`
- Apply labels: `gh issue edit <number> --add-label "..."`
- Remove labels: `gh issue edit <number> --remove-label "..."`
- Close an issue: `gh issue close <number> --comment "..."`

Infer the repository from `git remote -v` when running inside this clone.

## When a skill says "publish to the issue tracker"

Create a GitHub issue in `fenixfu/dousha`.

## When a skill says "fetch the relevant ticket"

Run `gh issue view <number> --comments`, or use the GitHub connector to fetch the issue and comments.
