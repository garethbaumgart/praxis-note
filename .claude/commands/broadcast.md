# Broadcast Feature Notification

Generate a feature notification data migration for the current PR.

## Process

### Step 1: Analyze PR Changes

Run these commands to understand the changes:
- `git diff main...HEAD` to see all changes in this branch
- `gh pr view --json title,body,number` to get PR details (if PR exists)

### Step 2: Determine Notification Type

Based on changes, classify as one of:
- `Feature`: New functionality added
- `BugFix`: Bug or issue fixed
- `Improvement`: Enhancement to existing functionality

### Step 3: Generate Migration File

Create a SQL file in `src/PraxisNote.Infrastructure/Migrations/FeatureNotifications/`

**Naming convention**: `YYYYMMDDHHMMSS_<BranchName>.sql`

**File content**:
```sql
INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
VALUES (
    '<Feature|BugFix|Improvement>',
    '<PR title or concise feature name>',
    '<1-2 sentence description of user-facing change>',
    'https://github.com/garethbaumgart/praxis-note/pull/<PR_NUMBER>',
    '<current UTC timestamp in ISO 8601 format>'
);
```

Note: The `Id` column is auto-incremented, so it should NOT be specified in the INSERT.

### Step 4: Guidelines for Writing Summary

- Focus on what users can now do, not technical implementation
- Keep under 200 characters
- Start with action verb: "Added...", "Fixed...", "Improved..."
- Example: "Added notification system to keep you updated on new features and bug fixes"

### Step 5: Commit the Migration

- Stage the new SQL file
- Commit with message: "Add feature notification for <feature-name>"
- Push to remote

## When to Skip

Do not create a notification for:
- Internal refactoring with no user-visible changes
- Test-only changes
- Documentation-only changes
- CI/workflow changes
- Dependency updates (unless they fix a user-facing issue)
