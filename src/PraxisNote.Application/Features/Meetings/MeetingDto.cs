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
    string? ReflectionData,
    DateTimeOffset? ReflectionSubmittedAt,
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

public record ReflectionPromptDto(
    string PromptId,
    string Category,
    string PromptText,
    IReadOnlyList<string> QuickOptions);

public record ReflectionDto(
    int? SelfAssessedTalkTime,
    string? SelfAssessedEngagement,
    string? SelfAssessedTone,
    string? InterruptionAwareness,
    string? FreeformReflection,
    IReadOnlyList<PromptResponseDto> PromptResponses);

public record PromptResponseDto(
    string PromptId,
    string PromptText,
    string Response);
