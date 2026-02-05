---
name: pr
description: Create or update a pull request. Use when the user wants to create a PR, submit changes for review, or merge their work. Handles git operations, tests, CI monitoring, and PR creation.
---

# Create Pull Request

You are creating or updating a pull request. Follow these steps in order.

## Step 1: Check for Uncommitted Changes

Run `git status` to check for uncommitted changes. If there are changes:
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

## Step 2.5: Generate Feature Notification (Optional)

If this PR includes user-facing changes (new features, bug fixes, or improvements), ask the user:
"Would you like to notify users about this change? (Run /broadcast)"

If yes, run the `/broadcast` skill to generate a notification migration, then continue.

Skip this step for:
- Internal refactoring
- Test changes
- Documentation updates
- CI/workflow changes

## Step 3: Run Tests

Run these checks and **ensure they pass**:

1. **Unit tests**: Execute `dotnet test` - expect 185+ tests, all must pass
2. **E2E tests**: Execute `cd tests/PraxisNote.E2E.Tests && npm test` - all tests must pass

**STOP if any tests fail.** Fix the failures and re-run until all tests pass. Do not proceed to PR creation with failing tests.

## Step 4: Create the PR

Once tests pass:

1. Push any remaining commits to the remote branch
2. Create the PR using `gh pr create`

## Step 5: Browser Validation (While CI Runs)

**Purpose**: Validate UI changes work correctly and capture visual evidence for review.

**Skip this step ONLY for**:
- Markdown-only PRs (`.md` files only)
- Backend-only changes with no UI impact
- Configuration or CI workflow changes

**For PRs with UI changes**:

1. **Start the dev stack**: Run `docker compose --profile dev-stack up -d`
2. **Wait for startup**: Wait for the app to be available at http://localhost:4200
3. **Navigate to the app**: Use the browser automation tools to open the app
4. **Capture before/after screenshots** (for refactoring PRs):
   - If this is a refactoring PR with no expected visual changes, take screenshots BEFORE making changes (from main branch) and AFTER
   - Compare to verify no unintended visual differences
   - Include screenshots in the PR description or comments
5. **Test each UI change**: For every UI-visible change in this PR:
   - Navigate to the affected area
   - Verify the change works as expected
   - **Take a screenshot** of the working feature
   - Test both light and dark mode if styling is involved (screenshot both)
   - Check responsive behavior if layout changes are involved
   - Test keyboard navigation if interactive elements are added
6. **Add screenshots to PR**: 
   - Use `gh pr comment` to add screenshots showing the UI works
   - For refactoring: "No visual changes - before/after comparison attached"
   - For new features: "Feature working as expected - screenshots attached"
7. **Fix any issues**: If something doesn't work or looks wrong, fix it, commit, push, and re-run tests

**Screenshot requirements by PR type**:
| PR Type | Required Screenshots |
|---------|---------------------|
| Refactoring (no visual change expected) | Before/after comparison from same view |
| New UI feature | Feature in action (light + dark mode if styled) |
| Bug fix with UI impact | Fixed state showing correct behavior |
| Styling/theming changes | Light mode + dark mode + mobile viewport |

**If UI validation fails**: Fix the issue, commit, push, and restart from Step 3.

## Step 6: Post-PR Review and Monitoring

After the PR is created, **actively monitor** and address feedback:

1. **Self code review**: Review the PR diff using `gh pr diff` and look for:
   - Code duplication that could be extracted (DRY principle)
   - Performance improvements without added complexity
   - Patterns that don't match existing codebase conventions
   - Missing null guards or error handling
   - Accessibility issues (missing aria-labels on icon-only buttons)
   - **Migration safety** (if migrations are included):
     - No migrations that exist in `main` branch were modified (merged migrations are immutable)
     - New migrations have been reviewed for accuracy (especially renames vs drop+add)
     - Destructive changes (DROP TABLE, DROP COLUMN) have been evaluated for data loss

   **Apply good refactoring opportunities** you identify - don't defer them to future PRs unless they require significant architectural changes. Add comments for any issues found using `gh pr comment` or `gh api`
2. **Wait for CI**: Monitor GitHub Actions for completion using `gh pr checks`
3. **Check for warnings**: Review action logs AND annotations for any warnings (not just failures)
   - Use `gh api repos/{owner}/{repo}/check-runs/{job_id}/annotations` to fetch annotations
   - Common warnings: deprecation notices, bundle size budgets, artifact upload failures, EF Core model validation
   - **ALL warnings must be addressed** - either fix the issue or update the workflow if it's a false positive
4. **Monitor for AI reviews**: Actively poll for CodeRabbit and Copilot reviews to complete
   - **CodeRabbit**: Use `gh pr checks` - wait until CodeRabbit shows "Review completed"
   - **Copilot**: Use `gh api repos/{owner}/{repo}/pulls/{number}/reviews --jq '.[] | select(.user.login | contains("copilot")) | .state'` to check if Copilot has submitted a review (look for "COMMENTED" state)
   - Alternatively, use `gh pr view <number> --comments` and look for comments from `copilot-pull-request-reviewer[bot]`
   - Keep checking every 30-60 seconds until BOTH CodeRabbit AND Copilot reviews are complete
5. **Address all comments immediately**: When comments appear:
   - Read each comment carefully, including **high-level feedback** in comment bodies (not just line-specific suggestions)
   - **For line comments (have their own ID)**:
     - **If addressing**: Add a thumbs up reaction using `gh api repos/{owner}/{repo}/pulls/comments/{comment_id}/reactions -X POST -f content='+1'`, then make the fix
     - **If not addressing**: Reply to the comment explaining why (must be a strong justification - see below)
   - **For high-level feedback in PR comments**: Reply to the comment addressing each suggestion

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
6. **Verify CI passes**: After all fixes, ensure all checks pass (no warnings in annotations)
7. **Wait for re-reviews after pushing fixes**: Every time you push new commits (from self-review fixes, addressing reviewer comments, or any other changes), you MUST restart the review monitoring loop:
   - Note the SHA of the latest commit you pushed
   - **Wait for Copilot to re-review the new commit**: Poll using `gh api repos/{owner}/{repo}/pulls/{number}/reviews --jq '.[] | select(.user.login | contains("copilot")) | {state, commit_id: .commit_id}'` and verify a review exists for the latest commit SHA. Copilot reviews against older commits do NOT count.
   - **Wait for CodeRabbit**: Check `gh pr checks` until CodeRabbit shows "Review completed"
   - **Address any new comments** from the re-review (repeat steps 5-7 as needed)
   - This loop continues until: the latest pushed commit has been reviewed by ALL reviewers, all comments are addressed, and CI is green

**Do not stop monitoring until**: The latest commit has been reviewed by ALL AI reviewers (both CodeRabbit AND Copilot must have reviews against the most recent commit SHA), all comments are addressed, and CI is green. It is NOT sufficient that reviewers reviewed an earlier commit — they must review the final state of the code.

## Step 7: User Approval and Merge

Once CI is green and all comments are addressed:

1. **Notify the user**: Tell them the PR is ready for their review and approval
2. **Wait for approval**: Do NOT merge until the user explicitly approves
3. **If feedback given**: Make fixes, commit, push, and repeat from Step 3 (tests + browser validation)
4. **If approved**: Proceed to merge with `gh pr merge --squash --delete-branch`

**Exception**: For markdown-only PRs (`.md` files only), merge immediately without waiting for user approval.
