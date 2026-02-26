using PraxisNote.Domain.Aggregates.DriveConnections;

namespace PraxisNote.Application.Features.Drive;

public sealed class GetDriveConnectionStatus(IDriveConnectionRepository repository)
{
    public record Query(Guid UserId, Guid ProfileId);
    public record Result(bool IsConnected, string? Provider, DateTimeOffset? ConnectedAt, DateTimeOffset? LastSyncedAt, string? FolderName);

    public async Task<Result> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var connection = await repository.GetByUserIdAsync(query.UserId, query.ProfileId, cancellationToken);

        if (connection is null)
            return new Result(false, null, null, null, null);

        return new Result(true, connection.Provider, connection.ConnectedAt, connection.LastSyncedAt, connection.FolderName);
    }
}
