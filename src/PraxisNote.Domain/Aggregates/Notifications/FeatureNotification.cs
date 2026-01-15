using PraxisNote.Domain.ValueObjects;

namespace PraxisNote.Domain.Aggregates.Notifications;

/// <summary>
/// Represents a system-wide notification about new features, bug fixes, or improvements.
/// These are read-only entries inserted via migrations.
/// </summary>
public sealed class FeatureNotification
{
    /// <summary>
    /// Auto-incrementing primary key.
    /// </summary>
    public int Id { get; private init; }

    /// <summary>
    /// The type of notification (Feature, BugFix, Improvement).
    /// </summary>
    public NotificationType Type { get; private init; }

    /// <summary>
    /// Brief title describing the change.
    /// </summary>
    public string Title { get; private init; } = string.Empty;

    /// <summary>
    /// Summary of what changed, focused on user impact.
    /// </summary>
    public string Summary { get; private init; } = string.Empty;

    /// <summary>
    /// Optional URL to the GitHub issue or PR for more details.
    /// </summary>
    public string? IssueUrl { get; private init; }

    /// <summary>
    /// When this notification was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>
    /// Required for EF Core.
    /// </summary>
    private FeatureNotification() { }

    /// <summary>
    /// Creates a new feature notification. Used by migrations for seeding data.
    /// </summary>
    public static FeatureNotification Create(
        NotificationType type,
        string title,
        string summary,
        string? issueUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);

        return new FeatureNotification
        {
            Type = type,
            Title = title.Trim(),
            Summary = summary.Trim(),
            IssueUrl = string.IsNullOrWhiteSpace(issueUrl) ? null : issueUrl.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
