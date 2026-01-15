using PraxisNote.Domain.Aggregates.Notifications;
using PraxisNote.Domain.Aggregates.Users;

namespace PraxisNote.Application.Features.Notifications;

public sealed class GetNotifications(
    INotificationRepository notificationRepository,
    IUserRepository userRepository)
{
    public record Query(Guid UserId);

    public async Task<IReadOnlyList<NotificationDto>> ExecuteAsync(
        Query query,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(query.UserId, cancellationToken);
        if (user is null)
        {
            return [];
        }

        var notifications = await notificationRepository.GetNotificationsForUserAsync(
            query.UserId,
            user.CreatedAt,
            cancellationToken);

        var seenIds = await notificationRepository.GetSeenNotificationIdsAsync(
            query.UserId,
            cancellationToken);

        return notifications
            .Select(n => new NotificationDto(
                n.Id,
                n.Type.ToString(),
                n.Title,
                n.Summary,
                n.IssueUrl,
                n.CreatedAt,
                seenIds.Contains(n.Id)))
            .ToList();
    }
}
