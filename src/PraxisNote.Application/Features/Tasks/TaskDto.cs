using PraxisNote.Application.Features.Tags;

namespace PraxisNote.Application.Features.Tasks;

public record TaskDto(
    Guid Id,
    string Title,
    string Status,
    int Position,
    bool IsPriority,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<CommentDto> Comments,
    DateOnly? DueDate,
    IReadOnlyList<TaskTagDto> Tags);

public record CommentDto(
    Guid Id,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
