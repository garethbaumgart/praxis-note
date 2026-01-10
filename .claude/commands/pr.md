# Create Pull Request

You are creating or updating a pull request. Follow these steps in order.

## Step 1: Check for Uncommitted Changes

Run `git status` to check for uncommitted changes. If there are changes:
- Stage and commit them with a clear, descriptive message
- Push to the remote branch

## Step 2: Run Tests

Run these checks and **ensure they pass**:

1. **Unit tests**: Execute `dotnet test` - expect 185+ tests, all must pass
2. **E2E tests**: Execute `cd tests/PraxisNote.E2E.Tests && npm test` - all tests must pass

**STOP if any tests fail.** Fix the failures and re-run until all tests pass. Do not proceed to PR creation with failing tests.

## Step 3: Create the PR

Once tests pass:

1. Push any remaining commits to the remote branch
2. Create the PR using `gh pr create`

## Step 4: Post-PR Review

After the PR is created, perform these checks:

1. **Self code review**: Review the PR diff using `gh pr diff` and add comments for any issues found using `gh pr comment` or `gh api`
2. **Wait for CI**: Monitor GitHub Actions for completion using `gh pr checks`
3. **Check for warnings**: Review action logs AND annotations for any warnings (not just failures)
   - Use `gh api repos/{owner}/{repo}/check-runs/{job_id}/annotations` to fetch annotations
   - Common warnings: deprecation notices, bundle size budgets, artifact upload failures, EF Core model validation
   - **ALL warnings must be addressed** - either fix the issue or update the workflow if it's a false positive
4. **Wait for Copilot**: Allow Copilot to complete its review
5. **Address all comments**: Fix any issues raised by Copilot or other reviewers
6. **Verify CI passes**: Ensure all checks pass after fixes (no warnings in annotations)

Only request merge approval once all comments are addressed, CI is green, and there are no warnings in annotations.
