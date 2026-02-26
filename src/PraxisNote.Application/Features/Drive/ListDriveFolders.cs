using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Drive.Services;
using PraxisNote.Domain.Aggregates.DriveConnections;

namespace PraxisNote.Application.Features.Drive;

public sealed class ListDriveFolders(
    IDriveConnectionRepository repository,
    IDriveService driveService,
    IUnitOfWork unitOfWork)
{
    public record Query(Guid UserId, Guid ProfileId, string? SearchQuery);
    public record FolderResult(string Id, string Name, DateTimeOffset? ModifiedTime);

    public async Task<IReadOnlyList<FolderResult>> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var connection = await repository.GetByUserIdAsync(query.UserId, query.ProfileId, cancellationToken)
            ?? throw new InvalidOperationException("No Google Drive connection found.");

        // Refresh token if expired (same pattern as SyncCalendarEvents)
        if (connection.IsTokenExpired())
        {
            var refreshResult = await driveService.RefreshAccessTokenAsync(connection.RefreshToken, cancellationToken);
            connection.UpdateTokens(refreshResult.AccessToken, refreshResult.ExpiresAt, refreshResult.RefreshToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var folders = await driveService.ListFoldersAsync(connection.AccessToken, query.SearchQuery, cancellationToken);
        return folders.Select(f => new FolderResult(f.Id, f.Name, f.ModifiedTime)).ToList();
    }
}
