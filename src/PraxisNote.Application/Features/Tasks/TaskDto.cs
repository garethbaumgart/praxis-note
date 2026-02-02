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
    IReadOnlyList<TaskTagDto> Tags,
    TaskSourceDto? Source = null);

/// <summary>
/// Represents the origin source of a task (meeting action item or note checkbox).
/// </summary>
public record TaskSourceDto(string Type, Guid Id, string Title);

public record CommentDto(
    Guid Id,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
