using System.Text.Json.Serialization;

namespace PraxisNote.Domain.Aggregates.Tasks;

/// <summary>
/// A comment on a task, used to track progress and notes.
/// Stored as JSONB array in the Tasks table.
/// </summary>
public sealed record Comment
{
    public Guid Id { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }

    [JsonConstructor]
    private Comment(Guid id, string content, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        Id = id;
        Content = content;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// Creates a new comment with the specified content.
    /// </summary>
    public static Comment Create(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var now = DateTimeOffset.UtcNow;
        return new Comment(Guid.NewGuid(), content.Trim(), now, now);
    }

    /// <summary>
    /// Returns a new comment with updated content.
    /// </summary>
    public Comment WithUpdatedContent(string newContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newContent);

        return this with
        {
            Content = newContent.Trim(),
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
