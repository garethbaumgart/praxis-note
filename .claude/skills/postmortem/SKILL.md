---
name: postmortem
description: Conduct a structured bug investigation and post-mortem analysis. Traces the root cause of production bugs, verifies deployment status, analyzes logs, identifies guardrail gaps, and produces actionable artifacts (bug fix issue + guardrail updates).
---

# Post-Mortem Bug Investigation

You are conducting a post-mortem investigation for a bug that was "fixed" but is still occurring in production. The goal is to trace the root cause, identify what the original fix missed, and produce artifacts to prevent recurrence.

## Step 1: Gather Context

Read the original bug report and any related issues/PRs.

```bash
# Find the original bug issue
gh issue view <bug-issue-number>

# Find related closed issues with similar keywords
gh issue list --state closed --search "<keyword>"

# Find PRs that claim to fix this bug
gh pr list --state merged --search "<keyword>"
```

**Extract:**
- Original bug description and symptoms
- Related issue numbers (e.g., "Closes #620")
- PR that merged the "fix"
- Commit SHA of the fix PR
- When the bug was reported vs when it was "fixed"

**Ask the user if needed:**
If the bug report doesn't reference a specific failed fix PR, ask: "Which PR attempted to fix this bug?"

## Step 2: Verify Deployment

Confirm that the fix commit is actually deployed to the environment where the bug still occurs.

### Cloud Run Deployment Check

If using Cloud Run, verify the deployed commit:

```bash
# Get the deployed image tag (usually a commit SHA)
gcloud run services describe praxisnote \
  --region australia-southeast1 \
  --format 'value(spec.template.spec.containers[0].image)'

# Compare against the fix PR's merge commit
gh pr view <pr-number> --json mergeCommit --jq '.mergeCommit.oid'
```

**Result:**
- If SHAs match: "✅ Fix commit IS deployed to production"
- If SHAs differ: "❌ Fix commit NOT deployed — deployment issue, not a code issue"

If the fix isn't deployed, stop here and report the deployment gap.

### Alternative: Check via Git Log

If Cloud Run access isn't available:

```bash
# Check if the fix commit is in the deployed branch
git log --oneline --all | grep <commit-sha>
```

## Step 3: Check Logs

Query production logs to find the actual error pattern.

### Cloud Run Logs

```bash
# Query logs for errors on the affected endpoint
gcloud logging read \
  "resource.type=cloud_run_revision AND resource.labels.service_name=praxisnote AND httpRequest.status=404 AND httpRequest.requestUrl=~\"api/notes.*promote\"" \
  --limit 50 \
  --format json \
  --freshness 7d
```

**Parse and summarize:**
- HTTP status codes (404, 500, etc.)
- Request paths that failed
- Timestamps (frequency of errors)
- Any exception messages in `jsonPayload.message`

**Example output format:**
```
Log Analysis:
- 15 requests to POST /api/notes/{id}/promote returned 404
- All occurred after the fix was merged (timestamps: 2026-02-18 14:32 - 2026-02-20 09:15)
- No 500 errors — endpoint not found, not a crash
```

### Alternative: Local Dev Logs

If Cloud Run access isn't available, check local dev server logs or ask the user for error details.

## Step 4: Trace Code Paths

From the error (e.g., 404 on `/api/notes/{id}/promote`), trace backwards through the codebase to find ALL code paths that touch the same operation.

### 4a: Find the Endpoint

```bash
# Find the API endpoint definition
grep -r "promote" src/PraxisNote.Web/Endpoints --include="*.cs" -A 5
```

### 4b: Find All Handlers

The key insight: there may be multiple handlers for the same entity (e.g., `UpdateNoteContent` vs `UpdateMeetingNote`). Find them all.

```bash
# Find all handlers that update notes
grep -r "class.*Note.*Handler" src/PraxisNote.Application --include="*.cs" -l

# Read each handler to see what domain methods they call
```

### 4c: Compare Code Paths

Create a comparison table showing which handlers call which domain methods.

**Example:**

| Handler | Calls `ExtractCheckboxes`? | Calls `UpdateContent`? |
|---------|----------------------------|------------------------|
| `UpdateNoteContent.Handler` | ✅ Yes (line 28) | ✅ Yes (line 35) |
| `UpdateMeetingNote.Handler` | ❌ No | ✅ Yes (line 42) |

**Result:**
```
Root Cause: The fix added checkbox extraction to `UpdateNoteContent.Handler`, but `UpdateMeetingNote.Handler` is a separate code path that was not updated.
```

## Step 5: Root Cause Analysis

Document:
1. **What the original fix did** — describe the code changes in the fix PR
2. **What the original fix missed** — identify the parallel code path, shared entity, or integration point that was overlooked
3. **Why it was missed** — explain the structural issue (e.g., "two handlers for the same entity", "feature flag hiding an alternate path")

**Example:**
```
## Root Cause

The original fix (PR #623) added checkbox extraction to `UpdateNoteContent.Handler` (line 28-35), which handles regular notes. However, meeting notes use a separate handler (`UpdateMeetingNote.Handler`) that directly calls `note.UpdateContent()` without extracting checkboxes first.

Both handlers modify the same `Note` aggregate, but only one path was updated.

## Why the Original Fix Missed This

1. The issue (#620) did not specify WHERE the promote-to-task button was being used (regular notes vs meeting notes)
2. The refinement did not grep for all callers of `note.UpdateContent()` to identify parallel code paths
3. The PR browser validation only tested the regular note editor, not the meeting note editor
```

## Step 6: Identify Guardrail Gaps

Review the skills and processes that allowed this bug to ship:

**Questions to ask:**
- Did `/refine` identify all callers of the modified method?
- Did the implementation plan list all code paths that save notes?
- Did `/pr` browser validation test BOTH contexts (regular notes AND meeting notes)?
- Did the acceptance criteria include verification contracts for all contexts?

**Output:**
```
## Guardrail Gaps

1. **Refinement**: The plan did not grep for all callers of `note.UpdateContent()` — it only looked at `UpdateNoteContent.Handler`.
   - Missing guardrail: "When modifying a domain method, find ALL application handlers that call it"

2. **PR Validation**: Browser testing only verified the regular note editor (/notes/{id}), not the meeting note editor (/meetings/{id}).
   - Missing guardrail: "When fixing a bug in entity X, verify the fix in ALL UI contexts where X is used"

3. **Acceptance Criteria**: No verification contract for meeting notes context.
   - Missing guardrail: "List all UI contexts/entry points for the affected feature in acceptance criteria"
```

## Step 7: Generate Artifacts

Create two outputs:

### 7a: Bug Fix GitHub Issue

```bash
gh issue create \
  --title "Fix promote-to-task in meeting note editor (missed code path from #620)" \
  --label "bug" \
  --body "$(cat <<'ISSUE_BODY'
## Post-Mortem Findings

**Original Issue:** #620 - Promote-to-task checkbox not working in note editor
**Original Fix:** PR #623 - Added checkbox extraction to `UpdateNoteContent.Handler`
**Deployed:** Yes (commit 2d25380 deployed to Cloud Run on 2026-02-18)
**Bug Status:** Still occurring in meeting note editor

## Root Cause

The fix only updated the regular note save path (`UpdateNoteContent.Handler`). Meeting notes use a separate handler (`UpdateMeetingNote.Handler`) that calls `note.UpdateContent()` directly without extracting checkboxes first.

**Evidence from Cloud Run Logs:**
- 15 POST requests to `/api/notes/{id}/promote` returned 404
- All occurred AFTER the fix was deployed (2026-02-18 to 2026-02-20)
- Endpoint missing from `MeetingEndpoints.cs`

## Code Path Comparison

| Handler | Extracts Checkboxes? | File |
|---------|---------------------|------|
| UpdateNoteContent.Handler | ✅ Yes (line 28) | `src/PraxisNote.Application/Features/Notes/UpdateNoteContent.cs` |
| UpdateMeetingNote.Handler | ❌ No | `src/PraxisNote.Application/Features/Meetings/UpdateMeetingNote.cs` |

Both handlers update the same `Note` aggregate, but only one was fixed.

## Fix Required

1. Add checkbox extraction to `UpdateMeetingNote.Handler` (same logic as `UpdateNoteContent.Handler`)
2. Add `/api/notes/{id}/promote` endpoint to `MeetingEndpoints.cs` (currently only in `NoteEndpoints.cs`)
3. Verify the promote button works in meeting note editor

## Acceptance Criteria

- [ ] Promote-to-task works in regular note editor
  - **Verify:** Create note at `/notes/new`. Type `[ ] task`. Click promote icon. Assert task appears in task list.

- [ ] Promote-to-task works in meeting note editor
  - **Verify:** Create meeting at `/meetings`. Add note via TipTap editor. Type `[ ] task`. Click promote icon. Assert task appears in task list.

- [ ] Endpoint exists for both contexts
  - **Verify:** Check `NoteEndpoints.cs` AND `MeetingEndpoints.cs` both register `POST /api/notes/{id}/promote`.

## Guardrail Updates

See post-mortem analysis for proposed guardrail improvements.

## Related

- Original issue: #620
- Original fix: PR #623
- Post-mortem skill: `/postmortem`
ISSUE_BODY
)"
```

### 7b: Guardrail Update Proposals

Present each proposed guardrail update to the user for approval:

```
I identified 3 guardrail gaps that allowed this bug to ship. I can update CLAUDE.md and/or the /refine skill to prevent recurrence.

**Proposed Update 1: CLAUDE.md - Pattern Examples**

Add to the "Backend Patterns" table:
| Pattern | Exemplar File |
|---------|--------------|
| Finding all callers of a domain method | `grep -r "methodName" src/PraxisNote.Application --include="*.cs" -A 3` |

**Proposed Update 2: /refine Step 5 - Exploration Techniques**

Current text (line 154-165):
```markdown
### Exploration Techniques

# Find all callers of a method
grep -r "methodName" src/ --include="*.ts" -l
```

Proposed addition (after line 165):
```markdown
# Find all APPLICATION HANDLERS that call a domain method
grep -r "methodName" src/PraxisNote.Application/Features --include="*.cs" -B 5 -A 10

When modifying a domain method (e.g., `note.UpdateContent()`), find ALL handlers that call it — not just the one referenced in the issue. Domain methods are often called from multiple contexts (regular vs meeting, public vs private, etc.).
```

**Proposed Update 3: /refine Step 6 - Plan Quality Requirements**

Add new subsection after "6. Verification Contracts" (line 306):

```markdown
#### 7. List All UI Contexts for the Feature

When fixing a bug or adding a feature to an entity (Task, Note, Meeting), list ALL UI contexts where that entity is edited or displayed. Verification contracts must test EACH context.

**Example:**
\`\`\`markdown
## UI Contexts for Notes

- Regular note editor (`/notes/{id}`)
- Meeting note editor (`/meetings/{id}` - embedded TipTap editor)
- Quick capture dialog (if applicable)

Each acceptance criterion must specify which contexts it applies to.
\`\`\`
```

Would you like me to apply these guardrail updates? (Yes to all / Choose individually / Skip)
```

Wait for user response, then apply approved updates.

## Step 8: Retrospective Questions

After generating artifacts, prompt the user with improvement questions:

```
## Retrospective Questions

1. Should `/refine` require grepping all application handlers when modifying a domain method, not just direct callers?

2. Should `/pr` browser validation require testing ALL UI contexts for the affected entity (e.g., regular notes AND meeting notes)?

3. Should acceptance criteria include a checklist of UI contexts to verify?

4. Should we add a rule: "When fixing context A, verify context B shares the same backend path"?

Let me know which (if any) you'd like to formalize as guardrails.
```

## Step 9: Summary

Report:
- Bug issue created (issue number + URL)
- Root cause identified
- Guardrail updates proposed (count)
- Next steps (implement the fix, apply guardrails)
