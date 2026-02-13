---
name: execute-issues
description: Execute a list of GitHub issues sequentially. Scans issues upfront, refines as needed, implements each one, creates PRs, and merges them in order.
---

# Execute GitHub Issues

You are executing a list of GitHub issues sequentially. The argument is a space-separated list of issue numbers (e.g., `/execute-issues 340 341 342`).

## Phase 1: Pre-Flight Scan

Before implementing anything, scan ALL listed issues upfront to assess readiness.

### Step 1: Assess Each Issue

For each issue number provided, run `gh issue view <number>` and check:
- Does the issue body contain an `## Implementation Plan` section?
- Is the scope clear enough to implement without clarification?
- Are there acceptance criteria?
- Is this a **trivial** change? (label changes, dependency bumps, one-line config fixes, docs-only)

### Step 2: Hard-Gate on Implementation Plans

**Issues without an `## Implementation Plan` section must NOT be autonomously refined.** This is a hard gate.

Classification rules:
- **Trivial issues** (label changes, dependency bumps, one-line fixes, docs-only): May proceed without a plan. These do not need `/refine`.
- **Non-trivial issues without a plan**: Must be surfaced to the user. Do NOT autonomously run `/refine` — the plan quality depends on human judgment for non-trivial work.

### Step 3: Check Plan Staleness

For issues that **have** an implementation plan, check if the plan is stale:

1. Extract all file paths mentioned in the plan's `## Implementation Plan` section.
2. Run `git log --oneline --since="$(gh issue view <number> --json updatedAt -q .updatedAt)" -- <file1> <file2> ...` to check if any referenced files were modified after the plan was written.
3. If files were modified, the plan is **stale** and must be flagged.

### Step 4: Present Summary

Present a summary table to the user:

```
| Issue | Title | Has Plan? | Trivial? | Stale? | Action |
|-------|-------|-----------|----------|--------|--------|
| #340  | ...   | Yes       | No       | No     | Ready to execute |
| #341  | ...   | No        | No       | N/A    | BLOCKED — needs /refine first |
| #342  | ...   | Yes       | No       | Yes    | STALE — files changed since plan was written |
| #343  | ...   | No        | Yes      | N/A    | Ready (trivial, no plan needed) |
```

### Step 5: User Decision

- **If any issues are BLOCKED** (non-trivial, no plan): Ask the user: "These issues need refinement before implementation. Please run `/refine` on them first, or confirm you'd like to skip them."
- **If any issues are STALE**: Ask the user: "These plans reference files that changed since the plan was written. Should I re-refine them, or proceed with the existing plan?"
- **If all issues are ready**: Tell the user all issues are ready and ask for confirmation to begin execution.

**Wait for the user's response before continuing to Phase 2.**

## Phase 2: Sequential Execution via Sub-Agents

Each issue is executed in its own **sub-agent** (using the `Task` tool with `subagent_type: "general-purpose"`). This gives each issue a fresh context window, preventing context exhaustion when executing many issues.

**IMPORTANT**: Issues must still run **sequentially** — wait for the sub-agent to complete and return its result before launching the next one.

For EACH issue in the provided order:

### Step 1: Read the Issue (in main agent)

Run `gh issue view <number>` to understand the requirements at a high level. Note:
- The issue title
- Whether it was flagged for refinement in Phase 1
- Whether it warrants a broadcast notification (new feature / significant improvement / user-facing bug fix → yes; minor bug fix / dependency update / test-only / CI/config / docs / internal refactoring → no)

### Step 2: Launch Sub-Agent

Use the `Task` tool to launch a sub-agent with the following configuration:

```
subagent_type: "general-purpose"
description: "Execute issue #<number>"
```

The prompt to the sub-agent MUST include ALL of the following context so it can work autonomously. Use this as a template (replace placeholders with actual values):

> You are implementing GitHub issue #NUMBER end-to-end. Work autonomously — do not ask the user any questions.
>
> **Issue Details:**
> PASTE_FULL_GH_ISSUE_VIEW_OUTPUT_HERE
>
> **Instructions:**
>
> 1. **Refine If Needed**: EITHER "Run the /refine NUMBER skill first to create an implementation plan. If /refine asks clarifying questions, use context from the issue body and codebase to answer with your best judgment." OR "Skip — this issue already has an implementation plan."
>
> 2. **Create a Feature Branch**: Run `git checkout main && git pull && git checkout -b feat/issue-NUMBER-SHORT_DESCRIPTION`
>
> 3. **Implement**: Follow the implementation plan step by step. Write the code, create tests as specified, and ensure everything compiles.
>
> 4. **Functional Verification**: Before creating the PR, manually verify the feature works by running the dev stack and using browser automation to exercise the primary user flow. For UI features, this means: navigate to the feature, interact with it (click, type, toggle), and verify the expected result appears in the DOM. If the feature does not work as expected, fix it before proceeding.
>
> 5. **Create PR and Merge**: Run the /pr skill to create a PR, run tests, monitor CI, address review comments, and merge. Override these /pr steps:
>    - /pr Step 3 (Broadcast): EITHER "Run /broadcast automatically (no user prompt) — this is a user-facing change." OR "Skip broadcast — this is a minor/internal change."
>    - /pr Step 10 (Merge Approval): Merge immediately without waiting for user approval. Do NOT ask the user.
>
> 6. **Verify and Clean Up**: After merge, confirm the PR state with `gh pr view --json state,number,url`. Then run `git checkout main && git pull`.
>
> 7. **Report Back**: When done, report a single summary line with: issue number, PR number, PR URL, and status (Merged/Failed). If you created a broadcast notification, mention that too.
>
> **Critical Rules:**
> - Do NOT ask the user any questions. Work fully autonomously.
> - If tests or CI fail, fix and retry. Only report failure if you are truly stuck after multiple attempts.
> - Broadcast decisions and merge approvals are autonomous — no user prompts.

### Step 3: Process Sub-Agent Result

When the sub-agent returns:
1. Parse its result to extract: PR number, PR URL, status, broadcast (yes/no)
2. Record the result for the final summary table
3. If the sub-agent reported failure, inform the user and ask whether to continue with the remaining issues or stop
4. State: `"Issue #<number> complete. Moving to issue #<next>."` or `"Issue #<number> complete. All issues finished."`
5. Proceed to the next issue or move to Phase 3

## Phase 3: Final Summary

After ALL issues are complete, present a final summary table:

```
| Issue | Title | PR | Status | Broadcast |
|-------|-------|----|--------|-----------|
| #340  | ...   | #N | Merged | Yes       |
| #341  | ...   | #N | Merged | Skipped (minor bug fix) |
| #342  | ...   | #N | Merged | Yes       |
```

Include links to each merged PR.

## Critical Rules

- **Sub-agent per issue** — each issue runs in its own sub-agent via the `Task` tool, giving it a fresh context window. The main agent orchestrates and tracks results.
- **SEQUENTIAL only** — never start issue N+1 until issue N's sub-agent has completed and returned its result
- **Fresh branch each time** — every issue branches off the latest `main` after pulling
- **Self-contained prompts** — the sub-agent prompt must include the full issue body and all instructions. The sub-agent cannot see the main conversation history.
- **Self-healing** — if tests or CI fail, the sub-agent should fix and retry. It should only report failure if truly stuck after multiple attempts.
- **Refine autonomously** — if `/refine` asks a clarifying question, answer with best judgment from the issue body and codebase. Only escalate to the user if genuinely undecidable.
- **No blocking prompts** — broadcast decisions, refine answers, and merge approvals should all be made autonomously. Neither the main agent nor sub-agents should ask the user for permission to merge.
