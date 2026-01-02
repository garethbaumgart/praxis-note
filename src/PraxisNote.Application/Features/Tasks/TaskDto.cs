namespace PraxisNote.Application.Features.Tasks;

public record TaskDto(
    Guid Id,
    string Title,
    string Status,
    int Position,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt);
