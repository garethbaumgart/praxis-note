namespace PraxisNote.Domain.ValueObjects;

/// <summary>
/// Represents the type of feature notification.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// A new feature has been added.
    /// </summary>
    Feature,

    /// <summary>
    /// A bug has been fixed.
    /// </summary>
    BugFix,

    /// <summary>
    /// An existing feature has been improved.
    /// </summary>
    Improvement
}
