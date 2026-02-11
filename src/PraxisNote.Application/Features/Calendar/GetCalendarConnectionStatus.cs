using PraxisNote.Domain.Aggregates.CalendarConnections;

namespace PraxisNote.Application.Features.Calendar;

public sealed class GetCalendarConnectionStatus(ICalendarConnectionRepository repository)
{
    public record Query(Guid UserId, Guid ProfileId);
    public record Result(bool IsConnected, string? Provider, DateTimeOffset? ConnectedAt, DateTimeOffset? LastSyncedAt);

    public async Task<Result> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var connection = await repository.GetByUserIdAndProviderAsync(query.UserId, query.ProfileId, "Google", cancellationToken);

        if (connection is null)
            return new Result(false, null, null, null);

        return new Result(true, connection.Provider, connection.ConnectedAt, connection.LastSyncedAt);
    }
}
