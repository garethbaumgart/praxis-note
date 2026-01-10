# Create Pull Request

This skill guides you through the complete PR workflow for this project.

## Pre-PR Checklist

Before creating the PR, run these checks:

1. **Run unit tests**: Execute `dotnet test` and verify all tests pass
2. **Run E2E tests**: Execute `cd tests/PraxisNote.E2E.Tests && npm test` and verify all tests pass

If any tests fail, fix them before proceeding. Never create a PR with failing tests.

## Create the PR

Once tests pass:

1. Commit all changes with a clear, descriptive message
2. Push to the remote branch
3. Create the PR using `gh pr create`

## Post-PR Workflow

After the PR is created:

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
