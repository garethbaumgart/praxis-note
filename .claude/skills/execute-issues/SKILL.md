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

## Phase 2: Sequential Execution

For EACH issue in the provided order, follow this exact workflow:

### Step 1: Read the Issue

Run `gh issue view <number>` to understand the requirements. Extract the goal, acceptance criteria, and implementation plan.

### Step 2: Refine If Needed

If the issue was flagged in Phase 1 as needing refinement, run `/refine <number>` to create an implementation plan before proceeding.

### Step 3: Create a Feature Branch

```bash
git checkout main && git pull && git checkout -b feat/issue-<number>-<short-description>
```

Use the issue title to derive the short description (lowercase, hyphens, max 5 words).

### Step 4: Implement

Follow the implementation plan step by step. Write the code, create tests as specified, and ensure everything compiles.

### Step 5: Run /pr

Execute the `/pr` skill to create a PR, run tests, monitor CI, address review comments, and get it merged.

**Override these /pr steps:**

#### /pr Step 3 Override (Broadcast)

Determine if the issue warrants a feature notification:

- **Run `/broadcast` automatically (no user prompt)** for: new features, significant improvements, user-facing bug fixes
- **Skip broadcast silently** for: minor bug fixes, dependency updates, test-only changes, CI/config changes, documentation-only changes, internal refactoring

Do NOT ask the user whether to broadcast. Make the decision based on the criteria above.

#### /pr Step 9 Override (Merge Approval)

- **All PRs**: Merge immediately without waiting for user approval

### Step 6: Verify Merge

Confirm the PR was merged:

```bash
gh pr view --json state
```

### Step 7: Return to Main

```bash
git checkout main && git pull
```

### Step 8: Report Progress

State one of:
- `"Issue #<number> complete. Moving to issue #<next>."` (if more issues remain)
- `"Issue #<number> complete. All issues finished."` (if this was the last one)

Then proceed to the next issue or move to Phase 3.

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

- **SEQUENTIAL only** — never start issue N+1 until issue N is fully merged
- **Fresh branch each time** — every issue branches off the latest `main` after pulling
- **Self-healing** — if tests or CI fail, fix and retry. Do not stop and ask unless you are truly stuck after multiple attempts.
- **Refine autonomously** — if `/refine` asks a clarifying question, use context from the issue body and codebase to answer it with your best judgment. Only escalate to the user if you genuinely cannot decide.
- **No blocking prompts in the loop** — broadcast decisions, refine answers, and merge approvals should all be made autonomously. Do not ask the user for permission to merge.
