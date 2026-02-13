---
name: refine
description: Refine and plan a GitHub issue for implementation. Reads the issue, explores the codebase, creates a UX mockup if needed, writes a step-by-step implementation plan, and updates the issue body.
---

# Refine Issue for Implementation

You are refining a GitHub issue into an actionable implementation plan. The argument is the issue number (e.g., `/refine 336`).

## Step 1: Read the Issue

```bash
gh issue view <number>
```

Read the full issue body. Extract:
- **Goal**: What needs to be built or changed
- **Acceptance criteria**: What "done" looks like
- **Design decision**: Any chosen mockup option or UX direction
- **Dependencies**: Other issues this depends on

## Step 2: Assess Clarity — Ask Clarifying Questions if Needed

Before exploring code, evaluate whether the issue is clear enough to plan:

**Ask clarifying questions if:**
- The issue has no acceptance criteria
- The scope is ambiguous ("improve the UI", "make it faster")
- There are multiple valid approaches and no design decision
- The issue references concepts not defined elsewhere

**Proceed without asking if:**
- The issue has clear acceptance criteria
- A design/mockup has already been chosen
- The scope and approach are unambiguous

Use `AskUserQuestion` to gather answers before continuing. Frame questions with specific options when possible.

## Step 3: Check for Existing Mockup

Look for a mockup in `mockups/` that matches the issue:

```bash
ls mockups/ | grep -i <feature-keyword>
```

**If a mockup exists AND has a chosen design**: Read the mockup file, note the chosen option, and incorporate it into the plan.

**If a mockup exists but NO design is chosen**: Read it, then ask the user which option they prefer before proceeding.

**If NO mockup exists AND the issue involves UI changes**: Create one (see Step 4).

**If NO mockup exists AND the issue is backend-only**: Skip to Step 5.

## Step 4: Create UX Mockup (When Needed)

Create a standalone HTML mockup file at `mockups/<feature-name>.html` with **10 distinct design options**.

### Mockup Template

Follow the exact pattern used in existing mockups. Key requirements:

1. **Use the project's semantic color tokens** — copy the CSS variable blocks from an existing mockup (e.g., `tag-hub-usage-counts.html`). The theme uses Nord palette colors with light/dark mode via `[data-theme="dark"]`.

2. **Include these standard elements:**
   - Tailwind CDN (`https://cdn.tailwindcss.com`)
   - PrimeIcons (`https://unpkg.com/primeicons@7.0.0/primeicons.css`)
   - Tailwind config mapping semantic colors to CSS variables
   - Theme toggle button (fixed top-right)
   - `toggleTheme()` script

3. **For each option include:**
   - Numbered option card with title and subtitle
   - Visual mockup of the design in context
   - Pros/cons list (green `#a3be8c` for pros, red `#bf616a` for cons)
   - Option dividers between each option

4. **Mark one option as "Recommended"** with:
   - Green border (`border: 2px solid #a3be8c`)
   - "Recommended" badge in the header
   - Clear reasoning for why it's recommended

5. **Include a comparison table** at the bottom summarizing all options across key dimensions.

6. **Include a "Recommendation" section** explaining the recommended choice.

### Mockup Structure Reference

```html
<!-- Copy the <head> section from an existing mockup for theme setup -->

<!-- Each option follows this pattern: -->
<div class="option-card mb-8"> <!-- Add class="recommended" for the recommended option -->
  <div class="option-header">
    <div class="flex items-center gap-3">
      <div class="option-number">1</div>
      <div>
        <div class="text-foreground text-sm font-semibold">Option Title</div>
        <div class="text-foreground-muted text-xs">Short description</div>
      </div>
    </div>
    <!-- Only on recommended: <div class="recommended-badge">Recommended</div> -->
  </div>
  <div class="option-body">
    <!-- Visual mockup content -->
    <div class="pros-cons">
      <div class="pro">+ Advantage</div>
      <div class="con">- Disadvantage</div>
    </div>
  </div>
</div>
<div class="option-divider"></div>
```

After creating the mockup, open it for the user:
```bash
open mockups/<feature-name>.html
```

Then tell the user: "I've created a mockup with 10 design options and recommended Option X. Please review and let me know which option you prefer, or I'll proceed with the recommended one."

Wait for the user's response before continuing.

## Step 5: Explore the Codebase

Thoroughly explore all code relevant to the issue. **Be exhaustive** — missing a file here means a broken plan.

### What to Find

**Backend (always check all of these):**
- Relevant domain entities/aggregates (look in `src/PraxisNote.Domain/Aggregates/`)
- Repository interfaces in Domain layer
- Repository implementations in Infrastructure layer (`src/PraxisNote.Infrastructure/Persistence/Repositories/`)
- Application layer handlers/queries/commands (`src/PraxisNote.Application/Features/`)
- DTOs and response models
- API endpoints (`src/PraxisNote.Web/Endpoints/`)
- Existing patterns to follow (e.g., how similar features were built)

**Frontend (always check all of these):**
- Models/interfaces (`*.model.ts`)
- Services (`*.service.ts`)
- Page components (`*.page.ts`)
- Shared components (`*.component.ts`)
- Template markup (inline templates in component files)

**Cross-cutting (always check):**
- Callers of methods being changed (use grep to find all call sites)
- Optimistic update patterns (look for `increment`/`decrement`/`update` calls on services)
- Existing tests for the area being modified (check `tests/` directory)
- Related mockups or design decisions

### Exploration Techniques

```bash
# Find relevant files by keyword
grep -r "MethodName" src/ --include="*.cs" --include="*.ts" -l

# Find all callers of a method
grep -r "methodName" src/ --include="*.ts" -l

# Check for existing tests
grep -r "ClassName" tests/ --include="*.cs" -l

# Understand the domain model
cat src/PraxisNote.Domain/Aggregates/<Entity>/<Entity>.cs
```

**Read the full contents** of every relevant file. Don't just find file names — read the actual code to understand:
- Method signatures and return types
- How data flows from API → handler → repository → database
- What patterns exist that should be followed
- What optimistic update hooks exist on the frontend

### Third-Party UI Extensions

When the plan involves integrating a UI library or extension, identify the actual DOM output by reading the library's source code or documentation. Do NOT assume standard HTML elements — many libraries (TipTap, ProseMirror, Slate, etc.) use custom NodeViews that render non-standard DOM structures. Include the actual DOM structure in the plan so CSS selectors, query selectors, and interaction logic are correct from the start.

## Step 6: Write the Implementation Plan

Write a concrete, step-by-step implementation plan with:

### Plan Structure

Each step must include:
1. **Step title** — what's being done
2. **Files table** — exact file paths and what changes in each
3. **Code snippets** — show the actual code to write (not pseudocode), referencing existing patterns

### Plan Rules

- **Order steps by dependency** — backend before frontend, interfaces before implementations
- **One concern per step** — don't mix repository changes with UI changes
- **Show the pattern to follow** — if there's existing code that does something similar, reference it explicitly (e.g., "Follow the same pattern as `TaskRepository.GetTagUsageCountsAsync` at line 28-42")
- **Include optimistic update changes** — if the change affects data that's optimistically updated on the frontend, include steps to update all callers
- **Include test expectations** — note what tests should be added or updated (but don't write the test code in the plan)

### Plan Quality Requirements (ENFORCED)

These requirements ensure sub-agents can implement the plan without ambiguity or missed updates.

#### 1. Exact Method Signatures

For any method being **created or modified**, the plan must include the full method signature — not just the file path. This prevents sub-agents from guessing parameter types, return types, or method names.

```markdown
**Method signature:**
\`\`\`csharp
// New method in ITaskRepository
Task<IReadOnlyList<TaskItem>> GetByDueDateRangeAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default);
\`\`\`

\`\`\`typescript
// New method in task.service.ts
loadByDueDateRange(from: string, to: string): void
\`\`\`
```

Do NOT write vague instructions like "add a method to get tasks by date range" — show the exact signature.

#### 2. List All Callers

For any method being **changed** (signature, behavior, or return type), the plan must identify **all call sites** by running grep and listing the results. Sub-agents must not discover missing callers mid-implementation.

```markdown
**Callers of `TaskRepository.GetByStatusAsync` (must be updated):**
- `src/PraxisNote.Application/Features/Tasks/GetUserTasks.cs:34`
- `src/PraxisNote.Application/Features/Summary/GetDailySummary.cs:58`
- `src/PraxisNote.Web/ClientApp/src/app/tasks/task.service.ts:42`
```

If a method has zero callers (new method), state that explicitly: "No existing callers — this is a new method."

#### 3. Template HTML Before/After

For UI changes, show the **exact HTML being modified** with before and after snippets. Do not write "update the template to show the new field" — show the actual markup.

```markdown
**Before** (from `tasks.page.ts` template, lines 45-52):
\`\`\`html
<div class="flex items-center gap-2">
  <span class="text-sm text-foreground">{{ task.title }}</span>
</div>
\`\`\`

**After:**
\`\`\`html
<div class="flex items-center gap-2">
  <span class="text-sm text-foreground">{{ task.title }}</span>
  @if (task.dueDate) {
    <span class="text-xs text-foreground-muted">{{ task.dueDate | date }}</span>
  }
</div>
\`\`\`
```

#### 4. List Shared Components to Reuse

Explicitly name shared components (with file paths) that the implementation should use, and **warn against creating duplicates**. Check `src/app/shared/components/` and `src/app/shared/services/` before planning any new shared utility.

```markdown
**Shared components to reuse:**
- `src/app/shared/components/error-state.component.ts` — use for API error display
- `src/app/shared/components/delete-confirm-button.component.ts` — use for inline delete
- `src/app/shared/services/toast.service.ts` — use for mutation success/error feedback

**Do NOT create:** A new error display component, toast wrapper, or confirmation dialog — these already exist.
```

#### 5. Pre-Flight Completeness Checklist

Append this machine-readable checklist to the end of every refined issue body. Every box must be checked before the issue is considered fully refined.

```markdown
## Pre-Flight Checklist

- [ ] All callers of modified methods identified
- [ ] Shared components listed (or confirmed none apply)
- [ ] Template HTML before/after included (for UI changes)
- [ ] Method signatures specified for new/modified methods
- [ ] Verification steps defined for each acceptance criterion
- [ ] Optimistic update impact assessed
- [ ] E2E test decision documented (add/skip with reason)
```

If a checklist item does not apply (e.g., "Template HTML" for a backend-only change), check it and add "N/A — backend only".

#### 6. Verification Contracts

Each acceptance criterion must have a concrete **verification contract** describing exactly what to navigate to, interact with, and assert during browser validation. This replaces vague criteria like "the feature works" with testable steps.

```markdown
## Acceptance Criteria

- [ ] Tasks can be filtered by due date range
  - **Verify:** Navigate to `/tasks`. Click the date filter. Select "This Week". Assert that only tasks with due dates in the current week are visible. Clear the filter. Assert all tasks reappear.

- [ ] Due date badge shows correct color
  - **Verify:** Create a task with due date = today. Navigate to `/tasks`. Assert the due date badge has class `text-danger`. Create a task with due date = next week. Assert the badge has class `text-foreground-muted`.
```

### Plan Template

```markdown
## Implementation Plan

### Step 1 — [Area]: [What]

**Files:**

| File | Change |
|------|--------|
| `path/to/file.cs` | Description of change |

**Method signatures** (new/modified):
\`\`\`csharp
Task<Result> NewMethodAsync(Guid userId, string title, CancellationToken ct = default);
\`\`\`

**Callers** (for modified methods):
- `path/to/caller1.cs:34` — update to pass new parameter
- `path/to/caller2.ts:58` — update return type handling

**Shared components to reuse:**
- `src/app/shared/components/error-state.component.ts` — for error display

**Pattern to follow** (from `path/to/existing.cs:28-42`):
\`\`\`csharp
// Existing code that shows the pattern
\`\`\`

**Template before/after** (for UI steps):
\`\`\`html
<!-- Before (line 45-52) -->
<div>...</div>

<!-- After -->
<div>...updated...</div>
\`\`\`

**New code:**
\`\`\`csharp
// The actual code to write
\`\`\`

### Step 2 — ...
```

## Step 7: Propose E2E Tests

After completing the implementation plan, assess whether the feature warrants E2E test coverage.

### Evaluation Criteria

**Propose a test if the feature involves:**
- Third-party library integration (custom DOM, NodeViews, plugins)
- New DOM structures not covered by existing tests
- Multi-step interaction flows (insert → interact → verify)
- Data persistence through complex UI paths (e.g., TipTap JSON ↔ DOM round-trip)

**Do NOT propose tests for:**
- Simple styling changes
- Individual formatting buttons (bold, italic) — tested upstream by the library
- Tooltip additions, icon changes
- Features fully covered by existing unit tests

If no tests meet the evaluation criteria, skip this step entirely — don't ask about tests that aren't worth writing.

### Presentation Flow — One Test at a Time

For each candidate E2E test, use `AskUserQuestion` to present it individually:

```
Question: "Should we add this E2E test?"
Header: "E2E Test"
Options:
  - "Yes" — Include this test in the implementation plan
  - "No" — Skip this test
  - (User can also select "Other" to describe modifications)

Description for the question should include:
  - Test name (e.g., "can insert and interact with toggle section")
  - Test file (e.g., smoke-tests/note-editor.spec.ts)
  - What it exercises (e.g., "Insert toggle via slash command → verify div[data-type='details'] renders → click toggle button → verify content shows/hides")
  - Why it's valuable (e.g., "Would have caught #493 — third-party NodeView mismatch")
```

Present tests **one at a time**, waiting for the user's response before presenting the next. This ensures each test gets individual consideration rather than being rubber-stamped as a batch.

### After All Tests Are Reviewed

- Add approved tests as implementation steps in the plan
- Note which tests were declined (for traceability)
- If the user modified a test, incorporate their feedback into the plan

## Step 8: Update Labels

After writing the plan, update the issue labels:
- **Remove** the `to-refine` label
- **Add** the `refined` label
- Add any relevant feature labels (e.g., `tag-hub`, `bug`)

```bash
gh issue edit <number> --remove-label "to-refine" --add-label "refined"
```

## Step 9: Update the GitHub Issue Body

Replace the issue body with the refined version. **Preserve** the original sections (Overview, Dependencies, Chosen Design) and **add/overwrite** the Implementation Plan section.

Use `gh issue edit` with a heredoc:

```bash
gh issue edit <number> --body "$(cat <<'ISSUE_BODY'
## Overview
[preserved from original]

## Dependencies
[preserved from original]

## Chosen Design
[preserved or updated based on mockup selection]

---

## Implementation Plan

### Step 1 — ...
[the full plan]

---

## Acceptance Criteria
[preserved or refined from original — each criterion includes a verification contract]

---

## Pre-Flight Checklist

- [ ] All callers of modified methods identified
- [ ] Shared components listed (or confirmed none apply)
- [ ] Template HTML before/after included (for UI changes)
- [ ] Method signatures specified for new/modified methods
- [ ] Verification steps defined for each acceptance criterion
- [ ] Optimistic update impact assessed
- [ ] E2E test decision documented (add/skip with reason)
ISSUE_BODY
)"
```

### What to Include in the Updated Issue

- **Overview** — keep the original, or refine for clarity
- **Dependencies** — preserve as-is
- **Chosen Design** — update if a mockup was created/selected in this session
- **Implementation Plan** — the full step-by-step plan from Step 6
- **Acceptance Criteria** — preserve original, add any new criteria discovered during planning. Each criterion must have a **verification contract** (see Plan Quality Requirements, item 6)
- **Pre-Flight Checklist** — the completeness checklist from Plan Quality Requirements, item 5

### What NOT to Include

- Internal exploration notes or reasoning
- Alternative approaches that were rejected
- Code that was read but not relevant to the plan

## Step 10: Summary

Tell the user:
1. What you found during exploration
2. How many steps the plan has
3. Any risks or decisions that need attention
4. Link to the updated issue
