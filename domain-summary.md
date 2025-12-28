# PraxisNote Domain Summary

A pure domain model for a note-first task management system designed for busy professionals.

---

## Aggregates Overview

| Aggregate | Purpose |
|-----------|---------|
| **User** | Authentication & ownership scoping |
| **Note** | Rich text content with embedded checkboxes |
| **Task** | Actionable items with status lifecycle |
| **Label** | Cross-cutting organization tags |

---

## 1. User Aggregate

**Purpose:** Represents an authenticated user who owns all notes, tasks, and labels.

### Entity: User

| Property | Type | Rules |
|----------|------|-------|
| Id | Guid | Immutable, generated |
| GoogleId | string | Immutable after creation, unique |
| Email | Email (VO) | Unique, valid format |
| Name | string | Required |
| AvatarUrl | string? | Nullable, from Google |
| CreatedAt | DateTime | Set on creation |
| LastLoginAt | DateTime | Updated on each login |

### Business Actions

| Action | Description | Business Rules |
|--------|-------------|----------------|
| `Register(googleId, email, name, avatarUrl)` | Create user from Google OAuth | GoogleId and Email must be unique |
| `UpdateProfile(name, avatarUrl)` | Update profile info | Name cannot be empty |
| `RecordLogin()` | Update last login timestamp | Sets LastLoginAt to now |

### Invariants
- Email must be valid format
- GoogleId is immutable after creation
- One user = one Google account (no password auth)

---

## 2. Note Aggregate

**Purpose:** Rich text content container that can hold embedded task checkboxes.

### Entity: Note

| Property | Type | Rules |
|----------|------|-------|
| Id | Guid | Immutable |
| UserId | Guid | FK → User, immutable |
| Content | NoteContent (VO) | TipTap JSON/HTML |
| Labels | List\<Label\> | Many-to-many via join |
| CreatedAt | DateTime | Set on creation |
| UpdatedAt | DateTime | Updated on any change |

### Business Actions

| Action | Description | Business Rules | Domain Event |
|--------|-------------|----------------|--------------|
| `Create(userId, content)` | Create new note | User must exist | `NoteCreated` |
| `UpdateContent(content)` | Change note content | Triggers checkbox sync | `NoteContentChanged` |
| `AddLabel(label)` | Tag note with label | Label must belong to same user | - |
| `RemoveLabel(label)` | Remove label from note | Does NOT remove from linked tasks | - |
| `Delete()` | Delete the note | Prompts for linked task handling | `NoteDeleted` |

### Delete with Linked Tasks
When deleting a note with linked tasks, the system offers two options:
1. **Delete tasks** - Remove all linked tasks
2. **Keep as standalone** - Unlink tasks (set CheckboxRef to null)

### Invariants
- Note belongs to exactly one User
- Content must be valid TipTap format (can be empty)
- Labels must belong to the same user as the note

---

## 3. Task Aggregate

**Purpose:** Actionable item with a three-state lifecycle (Todo → In Progress → Done).

### Entity: Task

| Property | Type | Rules |
|----------|------|-------|
| Id | Guid | Immutable |
| UserId | Guid | FK → User, immutable |
| Title | string | Required, non-empty |
| Status | TaskStatus (VO) | Todo, InProgress, Done |
| DueDate | DueDate? (VO) | Nullable date |
| CheckboxRef | CheckboxRef? (VO) | Links to source note/checkbox |
| Labels | List\<Label\> | Many-to-many via join |
| CreatedAt | DateTime | Set on creation |
| UpdatedAt | DateTime | Updated on any change |
| StartedAt | DateTime? | Set when entering InProgress |
| CompletedAt | DateTime? | Set when entering Done |

### Business Actions

| Action | Description | Business Rules | Domain Event |
|--------|-------------|----------------|--------------|
| `CreateStandalone(userId, title)` | Create task directly on board | Title required | `TaskCreated` |
| `CreateFromCheckbox(userId, title, checkboxRef, labels)` | Create from note checkbox | Inherits note's labels | `TaskCreated` |
| `Start()` | Move to In Progress | Only from Todo, sets StartedAt | `TaskStarted` |
| `Complete()` | Move to Done | From any status, sets CompletedAt | `TaskCompleted` |
| `Reopen()` | Move back to Todo | Clears StartedAt and CompletedAt | `TaskReopened` |
| `UpdateTitle(title)` | Change task title | Cannot be empty, syncs to note | `TaskTitleChanged` |
| `SetDueDate(date)` | Set or clear due date | Syncs to note if linked | `TaskDueDateChanged` |
| `ClearDueDate()` | Remove due date | - | `TaskDueDateChanged` |
| `AddLabel(label)` | Tag task with label | Label must belong to same user | - |
| `RemoveLabel(label)` | Remove label | - | - |
| `Delete()` | Delete the task | Removes checkbox from note if linked | `TaskDeleted` |

### Status State Machine

```
        Start()                Complete()
  ┌────────────────┐      ┌────────────────┐
  │                ▼      │                ▼
┌──────┐        ┌─────────────┐        ┌──────┐
│ TODO │        │ IN_PROGRESS │        │ DONE │
└──────┘        └─────────────┘        └──────┘
  ▲                │                     │
  │                │                     │
  └────────────────┴─────────────────────┘
              Reopen()
```

**Transition Rules:**
- Todo → InProgress: Sets `StartedAt`
- Todo → Done: Sets `CompletedAt` (skips InProgress)
- InProgress → Done: Sets `CompletedAt`, preserves `StartedAt`
- InProgress → Todo: Clears `StartedAt`
- Done → InProgress: Clears `CompletedAt`, sets `StartedAt` if null
- Done → Todo: Clears both `StartedAt` and `CompletedAt`

### Sorting Rules
- **Todo column:** By DueDate ascending (nulls at bottom)
- **In Progress column:** By StartedAt descending (most recent first)
- **Done column:** By CompletedAt descending (most recent first)

### Invariants
- Task belongs to exactly one User
- Title cannot be empty or whitespace
- Status transitions must follow state machine
- StartedAt only set when status is InProgress or Done
- CompletedAt only set when status is Done
- Labels must belong to the same user as the task

---

## 4. Label Aggregate

**Purpose:** Shared organizational tag that connects related notes and tasks.

### Entity: Label

| Property | Type | Rules |
|----------|------|-------|
| Id | Guid | Immutable |
| UserId | Guid | FK → User, immutable |
| Name | string | Unique per user |
| CreatedAt | DateTime | Set on creation |

### Business Actions

| Action | Description | Business Rules |
|--------|-------------|----------------|
| `Create(userId, name)` | Create new label | Name must be unique for user |
| `Rename(name)` | Change label name | Must remain unique for user |
| `Delete()` | Delete label | Removes from all notes and tasks |

### Invariants
- Label name is unique within a user's labels
- Label belongs to exactly one User
- Name cannot be empty

---

## 5. Value Objects

### TaskStatus
```csharp
public enum TaskStatus
{
    Todo,
    InProgress,
    Done
}

// Behavior
bool CanTransitionTo(TaskStatus newStatus); // Always true (all transitions allowed)
TaskStatus[] GetValidTransitions();
```

### NoteContent
```csharp
public record NoteContent(string RawContent)
{
    Checkbox[] GetCheckboxes();
    bool IsEmpty();
    NoteContent WithCheckboxChecked(string checkboxId, bool isChecked);
    NoteContent WithCheckboxText(string checkboxId, string newText);
    NoteContent WithoutCheckbox(string checkboxId);
}
```

### Checkbox (extracted from NoteContent)
```csharp
public record Checkbox(
    string Id,        // Unique within the note
    string Text,      // The checkbox label/title
    bool IsChecked    // Current state
);
```

### CheckboxRef
```csharp
public record CheckboxRef(Guid NoteId, string CheckboxId)
{
    bool IsLinked => NoteId != Guid.Empty;
}
```

### DueDate
```csharp
public record DueDate(DateOnly Date)
{
    bool IsOverdue() => Date < DateOnly.FromDateTime(DateTime.Today);
    bool IsDueSoon(int days) => Date <= DateOnly.FromDateTime(DateTime.Today.AddDays(days));
    int DaysUntilDue() => Date.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber;
    string ToDisplayString(); // "Today", "Tomorrow", "Overdue by 2d", etc.
}
```

### Email
```csharp
public record Email(string Value)
{
    // Invariant: Must match valid email format
    // Equality: Case-insensitive
}
```

---

## 6. Domain Services

### CheckboxSyncService

Handles bidirectional synchronization between note checkboxes and tasks.

| Method | Trigger | Action |
|--------|---------|--------|
| `SyncTasksFromNote(note, previousContent)` | NoteContentChanged | Diff checkboxes, create/update/delete tasks |
| `SyncCheckboxFromTask(task)` | TaskStatusChanged | Update checkbox checked state in note |
| `SyncTitleToNote(task)` | TaskTitleChanged | Update checkbox text in note |
| `SyncDueDateToNote(task)` | TaskDueDateChanged | Update checkbox due date in note |

**Sync Rules:**

| Scenario | Action |
|----------|--------|
| New checkbox added to note | Create Task, inherit all note labels |
| Checkbox removed from note | Delete linked Task |
| Checkbox text changed | Update Task title |
| Checkbox checked in note | Move Task to Done |
| Checkbox unchecked in note | Move Task to Todo |
| Task completed on board | Check checkbox in source note |
| Task reopened on board | Uncheck checkbox in source note |
| Task title edited on board | Update checkbox text in note |

### LabelInheritanceService

Handles label inheritance from notes to tasks.

| Method | Description |
|--------|-------------|
| `InheritLabels(task, note)` | Copy all note labels to new task |

**Rules:**
- Labels inherited at task creation time only
- Adding labels to note after tasks exist does NOT update existing tasks
- Removing labels from note does NOT remove from linked tasks

---

## 7. Domain Events

| Event | Raised By | Payload |
|-------|-----------|---------|
| `NoteCreated` | Note.Create() | NoteId, UserId |
| `NoteContentChanged` | Note.UpdateContent() | NoteId, OldContent, NewContent |
| `NoteDeleted` | Note.Delete() | NoteId, LinkedTaskIds |
| `TaskCreated` | Task.Create*() | TaskId, UserId, CheckboxRef? |
| `TaskStarted` | Task.Start() | TaskId |
| `TaskCompleted` | Task.Complete() | TaskId |
| `TaskReopened` | Task.Reopen() | TaskId |
| `TaskTitleChanged` | Task.UpdateTitle() | TaskId, OldTitle, NewTitle |
| `TaskDueDateChanged` | Task.SetDueDate() | TaskId, OldDate?, NewDate? |
| `TaskDeleted` | Task.Delete() | TaskId, CheckboxRef? |

---

## 8. Business Rules Summary

### User Scoping
- All entities (Note, Task, Label) belong to exactly one User
- Users can only see/edit their own data
- Cross-user data access returns "not found" (security)

### Task Extraction
- Any checkbox in a note creates a task
- No distinction between "regular" and "task" checkboxes
- Checkboxes created via toolbar, markdown `- [ ]`, or `/task` command all work the same

### Bidirectional Sync
- Changes on board reflect in source note
- Changes in note reflect on board
- Sync is near-real-time (on save)

### Label Inheritance
- Tasks inherit labels from parent note at creation
- Inheritance is one-time (not ongoing sync)
- Task labels can be modified independently after creation

### Note Deletion Handling
- Deleting a note with linked tasks prompts user
- Options: delete tasks OR keep as standalone
- User choice is respected

---

## 9. Test Scenarios

### Notes
- Create empty note
- Create note with content
- Edit note content
- Delete note without tasks
- Delete note with tasks (both options)
- Note auto-saves (UpdatedAt changes)

### Tasks
- Create standalone task
- Create task from checkbox
- Task status transitions (all 6 combinations)
- Edit task title
- Set/change/clear due date
- Delete task (standalone and linked)
- Task sorting by due date/started_at/completed_at

### Labels
- Create label
- Add label to note
- Add label to task
- Remove label from note
- Remove label from task
- Label name uniqueness per user
- Label inheritance on task creation

### Task Extraction & Sync
- Checkbox creates task on note save
- Task inherits note labels
- New checkbox in existing note creates new task
- Removed checkbox deletes linked task
- Edited checkbox text updates task title
- Checking checkbox completes task
- Unchecking checkbox reopens task
- Completing task on board checks checkbox in note
- Reopening task on board unchecks checkbox in note

### User Scoping
- User can only see own notes
- User can only see own tasks
- User can only see own labels
- Cross-user access fails gracefully

---

## 10. Recommended Project Structure

```
PraxisNote.Domain/
├── Aggregates/
│   ├── Users/
│   │   ├── User.cs
│   │   └── Events/
│   │       └── UserLoggedIn.cs
│   ├── Notes/
│   │   ├── Note.cs
│   │   └── Events/
│   │       ├── NoteCreated.cs
│   │       ├── NoteContentChanged.cs
│   │       └── NoteDeleted.cs
│   ├── Tasks/
│   │   ├── Task.cs
│   │   └── Events/
│   │       ├── TaskCreated.cs
│   │       ├── TaskStarted.cs
│   │       ├── TaskCompleted.cs
│   │       ├── TaskReopened.cs
│   │       ├── TaskTitleChanged.cs
│   │       ├── TaskDueDateChanged.cs
│   │       └── TaskDeleted.cs
│   └── Labels/
│       └── Label.cs
├── ValueObjects/
│   ├── TaskStatus.cs
│   ├── NoteContent.cs
│   ├── Checkbox.cs
│   ├── CheckboxRef.cs
│   ├── DueDate.cs
│   └── Email.cs
├── Services/
│   ├── CheckboxSyncService.cs
│   └── LabelInheritanceService.cs
├── Events/
│   ├── IDomainEvent.cs
│   └── DomainEventBase.cs
└── Common/
    ├── Entity.cs
    ├── AggregateRoot.cs
    └── ValueObject.cs

PraxisNote.Domain.Tests/
├── Aggregates/
│   ├── UserTests.cs
│   ├── NoteTests.cs
│   ├── TaskTests.cs
│   └── LabelTests.cs
├── ValueObjects/
│   ├── TaskStatusTests.cs
│   ├── NoteContentTests.cs
│   ├── DueDateTests.cs
│   └── EmailTests.cs
└── Services/
    ├── CheckboxSyncServiceTests.cs
    └── LabelInheritanceServiceTests.cs
```

---

## Next Steps

1. **Create GitHub repo** with this structure
2. **Implement Value Objects first** (they have no dependencies)
3. **Implement Aggregates** with business rules
4. **Add Domain Services** for cross-aggregate logic
5. **Write tests** for each invariant and business rule
