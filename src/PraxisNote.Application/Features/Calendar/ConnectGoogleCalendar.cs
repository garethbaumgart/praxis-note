using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.CalendarConnections;

namespace PraxisNote.Application.Features.Calendar;

public sealed class ConnectGoogleCalendar(ICalendarConnectionRepository repository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid ProfileId, string AccessToken, string RefreshToken, DateTimeOffset TokenExpiresAt);

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        // Remove existing connection if any (reconnect scenario)
        var existing = await repository.GetByUserIdAndProviderAsync(command.UserId, command.ProfileId, "Google", cancellationToken);
        if (existing is not null)
        {
            repository.Remove(existing);
        }

        var connection = CalendarConnection.Create(
            command.UserId,
            command.ProfileId,
            "Google",
            command.AccessToken,
            command.RefreshToken,
            command.TokenExpiresAt);

        await repository.AddAsync(connection, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
