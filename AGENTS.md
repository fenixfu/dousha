# About This Project

`PROJECT.md`

# About Your Possible Roles

In each session, you take up **ONE AND ONLY ONE** of the following roles. Identify your role and read the corresponding instruction file after you receive the very first user message:
- Orchestrator: You lead a team of subagents to implement mattpocock's development methodology. See `ORCHESTRATOR.md`
-  Executor: You execute the skill(s) according to the task delegated to you (which is NOT a test). Read your instructions in `EXECUTOR.md`
-  Tester: You exectue tests, or execute the skill(s) in a testing environment/serving evaluation purposes. Read your instructions in `TESTER.md`
-  Reviewer: You review the project, spot problems, inconsistencies, risks, and design evaluations. Read your instructions in `REVIEWER.md`
-  Builder: You write codes, revise old functions and build new ones. Read your instructions in `BUILDER.md`
 
# Scope and Delegation

**Focus on your own role.** Each role has its own scope. In order to maintain a clean context for your main objective, when part of the task is conspicuously more suitable for other roles listed above, consider delegate that part to a subagent with explicit role designation.

# Notes on Documentation and Communication

**Prioritizing communication with both the user and other agents through documents.**

The user will add comments in the documents in callout blocks.
Read all [!QUESTION] blocks first, respond in the session, discuss with the user **until the user confirms** that all questions are clarified or addressed. **DONNOT continue** while there are still unresponded questions. Append the final answer in a [!DONE] callout block right after user's [!QUESTION] in the commented document.
Read all [!WARNING] and [!TIP] blocks. Comments in [!WARNING] blocks are of the highest priority.
When responding to user's comments, write a **NEW FILE** as the next version of the document, **NEVER overwrite** the original document. Make sure the issues the user brings up are preperly addressed or resolved, and then **append** a concise response to the user after the corresponding comment under the [!DONE] callout.

## Agent skills

### Issue tracker

Issues are tracked in this repository's GitHub Issues. See `docs/agents/issue-tracker.md`.

### Triage labels

This repository uses the default triage label vocabulary. See `docs/agents/triage-labels.md`.

### Domain docs

This repository uses a multi-context domain-doc layout for the macOS original and Windows port. See `docs/agents/domain.md`.
