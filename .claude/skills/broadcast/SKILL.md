---
name: broadcast
description: Add a feature notification to announce new features, improvements, or bug fixes to users. Use when the user wants to broadcast a change, add a notification, or announce a new feature.
---

# Broadcast Feature Notification

Add a notification that appears in the "What's New" bell icon for all users.

## Steps

1. **Determine the notification type** based on the change:
   - `Feature` - New functionality
   - `Improvement` - Enhancement to existing feature
   - `BugFix` - Bug fix

2. **Get the PR number** from the current branch or ask the user

3. **Create a new migration** to insert the notification:
   - File: `src/PraxisNote.Infrastructure/Migrations/YYYYMMDDHHMMSS_AddNotification{Title}.cs`
   - Use the current timestamp for the migration name
   - Copy the Designer.cs from the most recent migration

4. **Migration template**:

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PraxisNote.Infrastructure.Migrations
{
    public partial class AddNotification{PascalCaseTitle} : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
                VALUES (
                    '{Type}',
                    '{Title}',
                    '{Summary}',
                    'https://github.com/garethbaumgart/praxis-note/pull/{PRNumber}',
                    '{ISO8601Timestamp}'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FeatureNotifications"
                WHERE "Title" = '{Title}';
                """);
        }
    }
}
```

5. **Copy the Designer.cs** from the most recent migration, updating:
   - The `[Migration("YYYYMMDDHHMMSS_AddNotification{Title}")]` attribute
   - The `partial class` name

6. **Build to verify**: `cd src && dotnet build`

7. **If dev-stack is running**, also insert directly into local DB:
```bash
docker exec praxisnote-db-dev psql -U praxisnote -d praxisnote -c "
INSERT INTO \"FeatureNotifications\" (\"Type\", \"Title\", \"Summary\", \"IssueUrl\", \"CreatedAt\")
VALUES ('{Type}', '{Title}', '{Summary}', 'https://github.com/garethbaumgart/praxis-note/pull/{PRNumber}', '{ISO8601Timestamp}');
"
```

## Guidelines

- **Title**: Short, action-oriented (e.g., "Task search and filtering", "Faster task archiving")
- **Summary**: One sentence describing the benefit to users
- **CreatedAt**: Use current UTC timestamp in ISO 8601 format (e.g., `2026-01-17T10:00:00Z`)

## Example

For PR #130 adding dark mode:

**Type**: Feature
**Title**: Dark mode support
**Summary**: Switch to a darker theme that's easier on the eyes in low-light environments.
**PR**: 130
