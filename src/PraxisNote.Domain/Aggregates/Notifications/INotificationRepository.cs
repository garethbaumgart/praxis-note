namespace PraxisNote.Domain.Aggregates.Notifications;

/// <summary>
/// Repository for feature notifications.
/// </summary>
public interface INotificationRepository
{
    /// <summary>
    /// Gets all feature notifications ordered by most recent first.
    /// </summary>
    Task<IReadOnlyList<FeatureNotification>> GetAllNotificationsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unseen notifications (Id > lastSeenNotificationId).
    /// </summary>
    Task<int> GetUnseenCountAsync(
        int? lastSeenNotificationId,
        CancellationToken cancellationToken = default);
}
