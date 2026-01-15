# Feature Notifications

This folder contains data migrations for feature notifications, separate from schema migrations.

## Naming Convention

Files are named with timestamp and feature: `YYYYMMDDHHMMSS_FeatureName.sql`

## Format

Each file contains a single INSERT statement:

```sql
INSERT INTO "FeatureNotifications" ("Id", "Type", "Title", "Summary", "IssueUrl", "CreatedAt")
VALUES (
    'guid-here',
    'Feature',
    'Feature Title',
    'Brief description of what changed',
    'https://github.com/owner/repo/issues/123',
    '2026-01-15T12:00:00Z'
);
```

## Generation

Use the `/broadcast` skill during PR flow to generate these files automatically.
