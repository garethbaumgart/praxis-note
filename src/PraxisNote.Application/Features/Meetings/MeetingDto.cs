namespace PraxisNote.Application.Features.Meetings;

public record MeetingDto(
    Guid Id,
    string? Title,
    DateTimeOffset? MeetingDate,
    string? Attendees,
    string? TranscriptContent,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
