using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Notifications;

namespace PraxisNote.Application.Features.Notifications;

public sealed class MarkNotificationsSeen(
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, IEnumerable<Guid> NotificationIds);

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        await notificationRepository.MarkAsSeenAsync(
            command.UserId,
            command.NotificationIds,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
