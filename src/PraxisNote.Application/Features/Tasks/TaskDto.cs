namespace PraxisNote.Application.Features.Tasks;

public record TaskDto(
    Guid Id,
    string Title,
    string Status,
    int Position,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<CommentDto> Comments);

public record CommentDto(
    Guid Id,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
