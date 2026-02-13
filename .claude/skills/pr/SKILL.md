---
name: pr
description: Create or update a pull request. Use when the user wants to create a PR, submit changes for review, or merge their work. Handles git operations, tests, CI monitoring, and PR creation.
---

# Create Pull Request

You are creating or updating a pull request. Follow these steps in order.

## Step 1: Verify Feature Branch and Check for Uncommitted Changes

**Branch protection rules prevent direct pushes to `main`.** All changes must go through a pull request.

1. Run `git branch --show-current` to check which branch you're on
2. If on `main`, create a feature branch first: `git checkout -b feat/short-description` (use `feat/`, `fix/`, `chore/`, or `docs/` prefix as appropriate)
3. Run `git status` to check for uncommitted changes. If there are changes:
   - Stage and commit them with a clear, descriptive message
   - Push to the remote branch

## Step 2: Review and Update README.md

Check if any changes in this PR require documentation updates:
- New features or commands
- Changed setup/installation steps (e.g., Docker commands)
- New environment variables or configuration
- Updated dev workflow
- API changes that affect usage examples

If updates are needed, make them and commit before proceeding.

## Step 3: Generate Feature Notification (Optional)

If this PR includes user-facing changes (new features, bug fixes, or improvements), ask the user:
"Would you like to notify users about this change? (Run /broadcast)"

If yes, run the `/broadcast` skill to generate a notification migration, then continue.

Skip this step for:
- Internal refactoring
- Test changes
- Documentation updates
- CI/workflow changes

## Step 4: Build and Test

### 4a: Build

Run `dotnet build` to verify the project compiles. If it fails (e.g., missing packages), run `dotnet restore` first, then retry. Fix any compilation errors before proceeding.

### 4b: Run Tests

Run these checks and **ensure they pass**:

1. **Unit tests**: Execute `dotnet test` — all tests must pass
2. **E2E tests**:
   - First ensure the E2E Docker profile is running: `docker compose --profile e2e up -d --wait`
   - Then execute: `cd tests/PraxisNote.E2E.Tests && npm test` — all tests must pass

**STOP if any tests fail.** Fix the failures and re-run until all tests pass. Do not proceed to PR creation with failing tests.

## Step 5: Self Code Review

Review the full diff against the base branch using `git diff main...HEAD` and look for:

### Code Quality
- Code duplication that could be extracted (DRY principle)
- Performance improvements without added complexity
- Patterns that don't match existing codebase conventions
- Missing null guards or error handling

### Security
- Missing `rel="noreferrer"` on external links (`target="_blank"`)
- Unsanitized user input
- Exposed secrets or credentials

### Frontend
- Race conditions in async service calls (e.g., stale responses overwriting newer ones)
- Timezone-safe date handling (use `new Date(year, month - 1, day)` for date-only strings, not `new Date(dateString)` which applies timezone offsets)
- Invalid date guards (check `isNaN(d.getTime())` before using parsed dates)
- Accessibility issues (missing `aria-label` on icon-only buttons, semantic HTML)

### Backend
- EF Core: JSON value conversion properties (e.g., `HashSet<Guid>`) cannot use `.Contains()` in LINQ-to-SQL — must filter in memory
- EF Core: `DbContext` is NOT thread-safe — never use `Task.WhenAll` with multiple repository calls sharing the same scoped DbContext
- **Migration safety** (if migrations are included):
  - No migrations that exist in `main` branch were modified (merged migrations are immutable)
  - New migrations have been reviewed for accuracy (especially renames vs drop+add)
  - Destructive changes (DROP TABLE, DROP COLUMN) have been evaluated for data loss

**Apply all fixes now.** Don't defer them — fix issues before creating the PR so reviewers see a clean initial diff. Commit fixes before proceeding.

## Step 6: Create the PR

Once tests pass and self-review fixes are committed:

1. Push any remaining commits to the remote branch
2. Create the PR using `gh pr create`

## Step 7: Browser Validation (While CI Runs)

**Purpose**: Validate UI changes work correctly and capture visual evidence for review.

**Skip this step ONLY for**:
- Markdown-only PRs (`.md` files only)
- Backend-only changes with no UI impact
- Configuration or CI workflow changes

**For PRs with UI changes**:

1. **Start the dev stack**: Run `docker compose --profile dev-stack up -d`
2. **Wait for startup**: Wait for the app to be available at http://localhost:4200
3. **Write a Playwright validation script**: Create a script in `tests/PraxisNote.E2E.Tests/` (where Playwright is installed) that uses Playwright to navigate to the app and capture screenshots. For PrimeNG components like dropdowns, interact with them directly (click to open, type to filter, click to select) rather than relying on URL query parameters.
4. **Capture before/after screenshots** (for refactoring PRs):
   - If this is a refactoring PR with no expected visual changes, take screenshots BEFORE making changes (from main branch) and AFTER
   - Compare to verify no unintended visual differences
   - Include screenshots in the PR description or comments
5. **Test each UI change**: For **all UI changes** (new features, modifications, refactors, styling changes, library upgrades), the validation script MUST interact with the affected functionality — not just screenshot the page. The script should: (1) trigger or navigate to the affected feature, (2) interact with it (click, type, toggle, hover — whatever the feature does), (3) assert the expected DOM changes occurred, (4) screenshot the result. A screenshot of a page where you haven't exercised the changed functionality is NOT valid validation.

   This applies to:
   - **New UI features**: Exercise the new functionality end-to-end
   - **UI modifications/refactors**: Exercise the existing functionality to verify it still works after the change
   - **CSS/styling changes**: Verify the affected elements render correctly and interactive states (hover, focus, active) still work
   - **Library upgrades**: Exercise all features that depend on the upgraded library to catch DOM structure or API changes

   For every UI-visible change in this PR:
   - Navigate to the affected area
   - Interact with the feature (click, type, toggle) and verify the expected result
   - **Take a screenshot** of the working feature
   - Test both light and dark mode if styling is involved (screenshot both)
   - Check responsive behavior if layout changes are involved
   - Test keyboard navigation if interactive elements are added
6. **Add screenshots to PR**:
   - Use `gh pr comment` to add screenshots showing the UI works
   - For refactoring: "No visual changes - before/after comparison attached"
   - For new features: "Feature working as expected - screenshots attached"
7. **Clean up**: Delete any temporary validation scripts after screenshots are captured
8. **Fix any issues**: If something doesn't work or looks wrong, fix it, commit, push, and re-run tests

**Screenshot requirements by PR type**:
| PR Type | Required Screenshots |
|---------|---------------------|
| Refactoring (no visual change expected) | Before/after comparison from same view |
| New UI feature | Feature in action (light + dark mode if styled) |
| Bug fix with UI impact | Fixed state showing correct behavior |
| Styling/theming changes | Light mode + dark mode + mobile viewport |

**If UI validation fails**: Fix the issue, commit, push, and restart from Step 4.

## Step 8: Acceptance Criteria Verification

If this PR references a GitHub issue, verify that the implementation satisfies all acceptance criteria.

1. **Read the linked issue's acceptance criteria** using `gh issue view <number>`
2. For each criterion:
   - Verify the implementation satisfies it (check the code, test results, or browser validation output)
   - Check off the criterion on the issue using `gh issue edit` to update the body with `[x]` replacing `[ ]`
   - If a criterion is NOT met, fix the implementation before proceeding
3. **Do NOT proceed to Step 9** until every acceptance criterion is checked off
4. If the issue has no acceptance criteria section, skip this step

This step applies to all PRs that reference a GitHub issue. The agent must go back to the issue and verify each criterion — not just assume the implementation is correct because tests pass.

## Step 9: Post-PR Monitoring and Review Comments

After the PR is created, **actively monitor** and address feedback:

1. **Wait for CI**: Monitor GitHub Actions for completion using `gh pr checks`
2. **Check for warnings**: Review action logs AND annotations for any warnings (not just failures)
   - Use `gh api repos/{owner}/{repo}/check-runs/{job_id}/annotations` to fetch annotations
   - Common warnings: deprecation notices, bundle size budgets, artifact upload failures, EF Core model validation
   - **ALL warnings must be addressed** - either fix the issue or update the workflow if it's a false positive
3. **Monitor for AI reviews**: Actively poll for CodeRabbit and Copilot reviews to complete
   - **CodeRabbit**: Use `gh pr checks` - wait until CodeRabbit shows "Review completed"
   - **Copilot**: Use `gh api repos/{owner}/{repo}/pulls/{number}/reviews --jq '.[] | select(.user.login | contains("copilot")) | .state'` to check if Copilot has submitted a review (look for "COMMENTED" state)
   - Alternatively, use `gh pr view <number> --comments` and look for comments from `copilot-pull-request-reviewer[bot]`
   - Keep checking every 5 minutes until BOTH CodeRabbit AND Copilot reviews are complete
4. **Address all comments immediately**: When comments appear:
   - Read each comment carefully, including **high-level feedback** in comment bodies (not just line-specific suggestions)
   - **For line comments (have their own ID)**:
     - **If addressing**: Add a thumbs up reaction using `gh api repos/{owner}/{repo}/pulls/comments/{comment_id}/reactions -X POST -f content='+1'`, then make the fix
     - **If not addressing**: Reply to the comment explaining why (must be a strong justification - see below)
   - **For high-level feedback in PR comments**: Reply to the comment addressing each suggestion

   **IMPORTANT - Batch Review Fixes**:
   Address ALL comments from a review round before committing. Collect all fixes, apply them, then commit and push once. This minimizes review cycles — each push triggers a new round of AI reviews, so fewer pushes means faster convergence.

   **IMPORTANT - No Deferring Valid Comments**:
   Valid review comments must be addressed in the current PR. Do NOT:
   - Create follow-up issues for feedback that can be fixed now
   - Say "will address in a future PR" for straightforward fixes
   - Defer refactoring suggestions that are clearly improvements

   The only acceptable reasons to not address a comment:
   - The suggestion is factually incorrect or based on a misunderstanding
   - The change would require significant architectural work outside PR scope
   - The suggestion conflicts with an established project pattern (cite the pattern)
   - The reviewer explicitly marked it as "nit" or "optional"

   If you find yourself wanting to defer, ask: "Can I fix this in under 30 minutes?" If yes, fix it now.
5. **Verify CI passes**: After all fixes, ensure all checks pass (no warnings in annotations)
6. **Wait for re-reviews after pushing fixes**: Every time you push new commits (from addressing reviewer comments or any other changes), you MUST restart the review monitoring loop:
   - Note the SHA of the latest commit you pushed
   - **Wait for Copilot to re-review the new commit**: Poll using `gh api repos/{owner}/{repo}/pulls/{number}/reviews --jq '.[] | select(.user.login | contains("copilot")) | {state, commit_id: .commit_id}'` and verify a review exists for the latest commit SHA. Copilot reviews against older commits do NOT count.
   - **Wait for CodeRabbit**: Check `gh pr checks` until CodeRabbit shows "Review completed"
   - **Polling timeout**: Poll every 2 minutes for up to 10 minutes. If a reviewer has not re-reviewed the latest commit after 10 minutes AND their previous review had no unaddressed comments, proceed to the next step — the reviewer likely has nothing new to add. Only continue waiting past 10 minutes if the reviewer's previous review contained comments that required code changes (i.e., there is a reasonable expectation of a follow-up review).
   - **Re-fetch ALL line-level comments**: After reviewers have reviewed the latest commit, fetch the full comment list using `gh api repos/{owner}/{repo}/pulls/{number}/comments` and check for any comments posted since your last comment check. Reviewers may post new comments on intermediate commits while you are working on fixes — checking only the review status is NOT sufficient. You must compare timestamps to find comments you haven't addressed yet.
   - **Address any new comments** from the re-review (repeat steps 4-6 as needed)
   - This loop continues until: the latest pushed commit has been reviewed by ALL reviewers, all comments are addressed, and CI is green

**Do not stop monitoring until**: CI is green, all line-level comments have been fetched and addressed, and either (a) all AI reviewers have reviewed the latest commit SHA, or (b) the 10-minute polling timeout has elapsed for reviewers whose previous round had no unaddressed comments.

## Step 10: User Approval and Merge

Once CI is green and all comments are addressed:

1. **Verify test plan completion**: Before requesting merge approval, verify that ALL test plan checkboxes in the PR description are checked. If any manual verification items remain unchecked, complete them first or convert them to automated checks. Do not proceed until every checkbox is marked done.
2. **Notify the user**: Tell them the PR is ready for their review and approval
3. **Wait for approval**: Do NOT merge until the user explicitly approves
4. **If feedback given**: Make fixes, commit, push, and repeat from Step 4 (build, tests + browser validation)
5. **If approved**: Proceed to merge with `gh pr merge --squash --delete-branch`

**Exception**: For markdown-only PRs (`.md` files only), merge immediately without waiting for user approval.
