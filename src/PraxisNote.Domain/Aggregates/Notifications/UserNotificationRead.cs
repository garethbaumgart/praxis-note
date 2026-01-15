using PraxisNote.Domain.Common;

namespace PraxisNote.Domain.Aggregates.Notifications;

/// <summary>
/// Tracks which notifications a user has seen.
/// </summary>
public sealed class UserNotificationRead : Entity
{
    /// <summary>
    /// The user who saw the notification.
    /// </summary>
    public Guid UserId { get; private init; }

    /// <summary>
    /// The notification that was seen.
    /// </summary>
    public Guid NotificationId { get; private init; }

    /// <summary>
    /// When the notification was marked as seen.
    /// </summary>
    public DateTimeOffset SeenAt { get; private init; }

    /// <summary>
    /// Required for EF Core.
    /// </summary>
    private UserNotificationRead() { }

    /// <summary>
    /// Creates a record that a user has seen a notification.
    /// </summary>
    public static UserNotificationRead Create(Guid userId, Guid notificationId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty, nameof(userId));
        ArgumentOutOfRangeException.ThrowIfEqual(notificationId, Guid.Empty, nameof(notificationId));

        return new UserNotificationRead
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            NotificationId = notificationId,
            SeenAt = DateTimeOffset.UtcNow
        };
    }
}
