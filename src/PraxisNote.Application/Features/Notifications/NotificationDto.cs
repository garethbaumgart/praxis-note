namespace PraxisNote.Application.Features.Notifications;

public record NotificationDto(
    int Id,
    string Type,
    string Title,
    string Summary,
    string? IssueUrl,
    DateTimeOffset CreatedAt,
    bool IsSeen);
