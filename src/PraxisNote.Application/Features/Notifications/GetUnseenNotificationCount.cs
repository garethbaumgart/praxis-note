using PraxisNote.Domain.Aggregates.Notifications;
using PraxisNote.Domain.Aggregates.Users;

namespace PraxisNote.Application.Features.Notifications;

public sealed class GetUnseenNotificationCount(
    INotificationRepository notificationRepository,
    IUserRepository userRepository)
{
    public record Query(Guid UserId);

    public async Task<int> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(query.UserId, cancellationToken);
        if (user is null)
        {
            return 0;
        }

        return await notificationRepository.GetUnseenCountAsync(
            query.UserId,
            user.CreatedAt,
            cancellationToken);
    }
}
