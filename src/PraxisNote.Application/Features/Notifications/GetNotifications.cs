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

        var notifications = await notificationRepository.GetAllNotificationsAsync(cancellationToken);
        var lastSeenId = user.LastSeenNotificationId;

        return notifications
            .Select(n => new NotificationDto(
                n.Id,
                n.Type.ToString(),
                n.Title,
                n.Summary,
                n.IssueUrl,
                n.CreatedAt,
                lastSeenId.HasValue && n.Id <= lastSeenId.Value))
            .ToList();
    }
}
