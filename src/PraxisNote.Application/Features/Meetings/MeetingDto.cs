using PraxisNote.Application.Features.Tags;

namespace PraxisNote.Application.Features.Meetings;

public record MeetingDto(
    Guid Id,
    string? Title,
    DateTimeOffset? MeetingDate,
    string? Attendees,
    string? TranscriptContent,
    string Status,
    string? Summary,
    string? KeyPoints,
    string? Decisions,
    string? BehavioralAnalysis,
    IReadOnlyList<MeetingTagDto> Tags,
    IReadOnlyList<ActionItemDto> ActionItems,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record MeetingTagDto(Guid Id, string Name);

public record ActionItemDto(
    Guid Id,
    string Description,
    string? Assignee,
    bool IsCompleted,
    DateTimeOffset CreatedAt);
