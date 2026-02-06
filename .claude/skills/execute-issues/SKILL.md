---
name: execute-issues
description: Execute a list of GitHub issues sequentially. Scans issues upfront, refines as needed, implements each one, creates PRs, and merges them in order.
---

# Execute GitHub Issues

You are executing a list of GitHub issues sequentially. The argument is a space-separated list of issue numbers (e.g., `/execute-issues 340 341 342`).

## Phase 1: Pre-Flight Scan

Before implementing anything, scan ALL listed issues upfront to assess readiness.

1. For each issue number provided, run `gh issue view <number>` and check:
   - Does the issue body contain an `## Implementation Plan` section?
   - Is the scope clear enough to implement without clarification?
   - Are there acceptance criteria?
   - Is this a minor change (bug fix, dependency update, config, docs, test-only)?

2. Present a summary table to the user:

```
| Issue | Title | Has Plan? | Needs Refinement? | Minor? | Reason |
|-------|-------|-----------|-------------------|--------|--------|
| #340  | ...   | Yes       | No                | No     |        |
| #341  | ...   | No        | Yes               | No     | No implementation plan |
| #342  | ...   | Yes       | Yes               | Yes    | Ambiguous acceptance criteria |
```

3. Ask the user: **"These issues need refinement before implementation. Should I proceed with refining them as part of the execution, or do you want to refine them separately first?"**

4. **Wait for the user's response before continuing to Phase 2.**

If no issues need refinement, tell the user all issues are ready and ask for confirmation to begin execution.

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
> 4. **Create PR and Merge**: Run the /pr skill to create a PR, run tests, monitor CI, address review comments, and merge. Override these /pr steps:
>    - /pr Step 3 (Broadcast): EITHER "Run /broadcast automatically (no user prompt) — this is a user-facing change." OR "Skip broadcast — this is a minor/internal change."
>    - /pr Step 9 (Merge Approval): Merge immediately without waiting for user approval. Do NOT ask the user.
>
> 5. **Verify and Clean Up**: After merge, confirm the PR state with `gh pr view --json state,number,url`. Then run `git checkout main && git pull`.
>
> 6. **Report Back**: When done, report a single summary line with: issue number, PR number, PR URL, and status (Merged/Failed). If you created a broadcast notification, mention that too.
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
