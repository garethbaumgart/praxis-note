using Microsoft.EntityFrameworkCore;
using PraxisNote.Domain.Aggregates.Notifications;

namespace PraxisNote.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository(PraxisNoteDbContext context) : INotificationRepository
{
    public async Task<IReadOnlyList<FeatureNotification>> GetNotificationsForUserAsync(
        Guid userId,
        DateTimeOffset userCreatedAt,
        CancellationToken cancellationToken = default)
    {
        // Get last notification before user signup
        var lastBeforeSignup = await context.FeatureNotifications
            .Where(n => n.CreatedAt < userCreatedAt)
            .OrderByDescending(n => n.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        // Get all notifications at or after signup
        var afterSignup = await context.FeatureNotifications
            .Where(n => n.CreatedAt >= userCreatedAt)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        // Combine results
        if (lastBeforeSignup != null)
        {
            afterSignup.Add(lastBeforeSignup);
        }

        return afterSignup.OrderByDescending(n => n.CreatedAt).ToList();
    }

    public async Task<IReadOnlySet<Guid>> GetSeenNotificationIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var ids = await context.UserNotificationReads
            .Where(r => r.UserId == userId)
            .Select(r => r.NotificationId)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    public async Task MarkAsSeenAsync(
        Guid userId,
        IEnumerable<Guid> notificationIds,
        CancellationToken cancellationToken = default)
    {
        var existingIds = await GetSeenNotificationIdsAsync(userId, cancellationToken);

        var newReads = notificationIds
            .Distinct()
            .Where(id => !existingIds.Contains(id))
            .Select(id => UserNotificationRead.Create(userId, id));

        await context.UserNotificationReads.AddRangeAsync(newReads, cancellationToken);
    }

    public async Task<int> GetUnseenCountAsync(
        Guid userId,
        DateTimeOffset userCreatedAt,
        CancellationToken cancellationToken = default)
    {
        var seenIds = await GetSeenNotificationIdsAsync(userId, cancellationToken);

        // Count notifications after signup that haven't been seen
        var afterSignupCount = await context.FeatureNotifications
            .Where(n => n.CreatedAt >= userCreatedAt && !seenIds.Contains(n.Id))
            .CountAsync(cancellationToken);

        // Check if the last notification before signup is unseen
        var lastBeforeSignup = await context.FeatureNotifications
            .Where(n => n.CreatedAt < userCreatedAt)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => n.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var beforeSignupUnseen = lastBeforeSignup != Guid.Empty && !seenIds.Contains(lastBeforeSignup) ? 1 : 0;

        return afterSignupCount + beforeSignupUnseen;
    }
}
