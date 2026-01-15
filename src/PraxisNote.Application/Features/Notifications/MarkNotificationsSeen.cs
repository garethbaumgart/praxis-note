using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Users;

namespace PraxisNote.Application.Features.Notifications;

public sealed class MarkNotificationsSeen(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, int LastSeenNotificationId);

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return;
        }

        user.UpdateLastSeenNotificationId(command.LastSeenNotificationId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
