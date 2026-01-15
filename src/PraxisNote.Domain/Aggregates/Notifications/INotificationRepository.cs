namespace PraxisNote.Domain.Aggregates.Notifications;

/// <summary>
/// Repository for feature notifications and user read tracking.
/// </summary>
public interface INotificationRepository
{
    /// <summary>
    /// Gets notifications visible to a user (created at or after their signup,
    /// plus the last one before signup).
    /// </summary>
    Task<IReadOnlyList<FeatureNotification>> GetNotificationsForUserAsync(
        Guid userId,
        DateTimeOffset userCreatedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notification IDs the user has already seen.
    /// </summary>
    Task<IReadOnlySet<Guid>> GetSeenNotificationIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks notifications as seen for a user.
    /// </summary>
    Task MarkAsSeenAsync(
        Guid userId,
        IEnumerable<Guid> notificationIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unseen notifications for a user.
    /// </summary>
    Task<int> GetUnseenCountAsync(
        Guid userId,
        DateTimeOffset userCreatedAt,
        CancellationToken cancellationToken = default);
}
