namespace PraxisNote.Application.Features.Summary;

public record DailySummaryDto(
    DateOnly Date,
    DailySummaryStats Stats,
    List<MeetingSummaryItem> Meetings,
    List<OutstandingActionItem> OutstandingActionItems,
    List<CompletedTaskItem> CompletedTasks,
    List<InProgressTaskItem> InProgressTasks,
    List<NoteActivityItem> NotesUpdated);

public record DailySummaryStats(
    int MeetingCount,
    int TasksCompleted,
    int TasksStarted,
    int ActionItemsOpen,
    int NotesUpdated);

public record MeetingSummaryItem(
    Guid Id,
    string? Title,
    DateTimeOffset? MeetingDate,
    string? Attendees,
    string Status,
    string? Summary,
    int ActionItemCount,
    int DecisionCount,
    int CompletedActionItemCount);

public record OutstandingActionItem(
    Guid ActionItemId,
    string Description,
    string? Assignee,
    Guid MeetingId,
    string? MeetingTitle,
    DateTimeOffset? MeetingDate,
    bool IsLinkedToTask,
    Guid? LinkedTaskId,
    string? LinkedTaskStatus);

public record CompletedTaskItem(
    Guid Id,
    string Title,
    bool IsPriority,
    DateTimeOffset? CompletedAt);

public record InProgressTaskItem(
    Guid Id,
    string Title,
    bool IsPriority,
    DateTimeOffset? StartedAt);

public record NoteActivityItem(
    Guid Id,
    string Title,
    DateTimeOffset UpdatedAt,
    bool IsNew);
