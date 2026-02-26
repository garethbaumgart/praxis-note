using PraxisNote.Domain.Aggregates.DriveConnections;
using PraxisNote.Domain.Aggregates.DriveFileImports;

namespace PraxisNote.Application.Features.Drive;

public sealed class GetPendingDriveImportCount(
    IDriveConnectionRepository connectionRepository,
    IDriveFileImportRepository fileImportRepository)
{
    public record Query(Guid UserId, Guid ProfileId);

    public async Task<int> ExecuteAsync(Query query, CancellationToken ct = default)
    {
        var connection = await connectionRepository.GetByUserIdAsync(query.UserId, query.ProfileId, ct);
        if (connection is null) return 0;

        return await fileImportRepository.GetPendingCountByConnectionAsync(connection.Id, ct);
    }
}
