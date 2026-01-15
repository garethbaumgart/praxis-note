using Microsoft.EntityFrameworkCore;
using PraxisNote.Domain.Aggregates.Notifications;

namespace PraxisNote.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository(PraxisNoteDbContext context) : INotificationRepository
{
    public async Task<IReadOnlyList<FeatureNotification>> GetAllNotificationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await context.FeatureNotifications
            .OrderByDescending(n => n.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnseenCountAsync(
        int? lastSeenNotificationId,
        CancellationToken cancellationToken = default)
    {
        if (lastSeenNotificationId is null)
        {
            return await context.FeatureNotifications.CountAsync(cancellationToken);
        }

        return await context.FeatureNotifications
            .Where(n => n.Id > lastSeenNotificationId)
            .CountAsync(cancellationToken);
    }
}
