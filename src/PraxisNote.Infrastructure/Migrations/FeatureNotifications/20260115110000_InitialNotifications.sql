-- Initial feature notifications seeding (10 recent features)

-- 1. Archive view for Done tasks (PR #79)
INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
VALUES (
    'Feature',
    'Archive view for Done tasks',
    'Access older completed tasks in a dedicated Archive view, keeping your Done column focused on recent work.',
    'https://github.com/garethbaumgart/praxis-note/pull/79',
    '2026-01-06T00:00:00Z'
);

-- 2. Faster task archiving (PR #80)
INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
VALUES (
    'Improvement',
    'Faster task archiving',
    'Done tasks now archive after 2 days instead of 7, keeping your board cleaner.',
    'https://github.com/garethbaumgart/praxis-note/pull/80',
    '2026-01-07T00:00:00Z'
);

-- 3. Clickable URLs in comments (PR #88)
INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
VALUES (
    'Feature',
    'Clickable URLs in comments',
    'URLs in task comments are now automatically converted to clickable links.',
    'https://github.com/garethbaumgart/praxis-note/pull/88',
    '2026-01-08T00:00:00Z'
);

-- 4. Expandable comments (PR #89)
INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
VALUES (
    'Feature',
    'Expandable comments',
    'Comments now collapse with a badge showing the count. Click to expand and view all comments.',
    'https://github.com/garethbaumgart/praxis-note/pull/89',
    '2026-01-09T00:00:00Z'
);

-- 5. Task search and filtering (PR #91)
INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
VALUES (
    'Feature',
    'Task search and filtering',
    'Added search bar to quickly find tasks by title or description across all columns.',
    'https://github.com/garethbaumgart/praxis-note/pull/91',
    '2026-01-10T00:00:00Z'
);

-- 6. +35 days due date shortcut (PR #100)
INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
VALUES (
    'Improvement',
    '+35 days due date shortcut',
    'Quickly set due dates 5 weeks out with the new +35 days button in the date picker.',
    'https://github.com/garethbaumgart/praxis-note/pull/100',
    '2026-01-11T00:00:00Z'
);

-- 7. Sort tasks by column (PR #101)
INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
VALUES (
    'Feature',
    'Sort tasks by column',
    'Sort tasks within each column by date created, due date, or priority using the new dropdown.',
    'https://github.com/garethbaumgart/praxis-note/pull/101',
    '2026-01-12T00:00:00Z'
);

-- 8. Skeleton loading (PR #104)
INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
VALUES (
    'Improvement',
    'Skeleton loading',
    'Task columns now show elegant skeleton placeholders while loading for a smoother experience.',
    'https://github.com/garethbaumgart/praxis-note/pull/104',
    '2026-01-13T00:00:00Z'
);

-- 9. Priority flag for tasks (PR #114)
INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
VALUES (
    'Feature',
    'Priority flag for tasks',
    'Mark important tasks with a priority flag to keep them at the top of your columns.',
    'https://github.com/garethbaumgart/praxis-note/pull/114',
    '2026-01-14T00:00:00Z'
);

-- 10. What's New notifications (PR #118)
INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
VALUES (
    'Feature',
    'What''s New notifications',
    'Stay updated on new features and improvements with the notification bell in the header.',
    'https://github.com/garethbaumgart/praxis-note/pull/118',
    '2026-01-15T11:00:00Z'
);

-- 11. Smarter task sorting (PR #125)
INSERT INTO "FeatureNotifications" ("Type", "Title", "Summary", "IssueUrl", "CreatedAt")
VALUES (
    'Improvement',
    'Smarter task sorting',
    'Priority and due date sorts now use each other as secondary sorts for smarter task ordering.',
    'https://github.com/garethbaumgart/praxis-note/pull/125',
    '2026-01-16T00:00:00Z'
);
