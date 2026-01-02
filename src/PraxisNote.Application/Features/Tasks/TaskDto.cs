namespace PraxisNote.Application.Features.Tasks;

public record TaskDto(
    Guid Id,
    string Title,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
