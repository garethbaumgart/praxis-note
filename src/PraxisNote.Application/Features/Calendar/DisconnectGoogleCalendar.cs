using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.CalendarConnections;

namespace PraxisNote.Application.Features.Calendar;

public sealed class DisconnectGoogleCalendar(ICalendarConnectionRepository repository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid ProfileId);

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var connection = await repository.GetByUserIdAndProviderAsync(command.UserId, command.ProfileId, "Google", cancellationToken);
        if (connection is null)
            return;

        repository.Remove(connection);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
