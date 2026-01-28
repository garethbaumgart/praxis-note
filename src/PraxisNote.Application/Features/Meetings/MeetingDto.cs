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
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
