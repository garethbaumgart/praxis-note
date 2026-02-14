namespace PraxisNote.Application.Features.ActionItems;

public record OutstandingActionItemDto(
    Guid ActionItemId,
    string Description,
    string? Assignee,
    Guid MeetingId,
    string? MeetingTitle,
    DateTimeOffset? MeetingDate,
    bool IsLinkedToTask,
    Guid? LinkedTaskId,
    string? LinkedTaskStatus);
