using System.Text.Json.Serialization;

namespace PraxisNote.Domain.Aggregates.Meetings;

/// <summary>
/// An action item extracted from a meeting transcript.
/// Stored as JSONB array in the Meetings table.
/// </summary>
public sealed record ActionItem
{
    public Guid Id { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? Assignee { get; init; }
    public bool IsCompleted { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    [JsonConstructor]
    private ActionItem(Guid id, string description, string? assignee, bool isCompleted, DateTimeOffset createdAt)
    {
        Id = id;
        Description = description;
        Assignee = assignee;
        IsCompleted = isCompleted;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Creates a new action item with the specified description and optional assignee.
    /// </summary>
    public static ActionItem Create(string description, string? assignee = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return new ActionItem(
            Guid.NewGuid(),
            description.Trim(),
            string.IsNullOrWhiteSpace(assignee) ? null : assignee.Trim(),
            isCompleted: false,
            DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Returns a new action item with toggled completion status.
    /// </summary>
    public ActionItem WithCompletedToggled() => this with { IsCompleted = !IsCompleted };
}
