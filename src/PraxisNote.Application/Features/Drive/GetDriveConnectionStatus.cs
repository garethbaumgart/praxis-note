using PraxisNote.Domain.Aggregates.DriveConnections;
using PraxisNote.Domain.Aggregates.DriveFileImports;

namespace PraxisNote.Application.Features.Drive;

public sealed class GetDriveConnectionStatus(
    IDriveConnectionRepository repository,
    IDriveFileImportRepository fileImportRepository)
{
    public record Query(Guid UserId, Guid ProfileId);

    public record Result(
        bool IsConnected,
        string? Provider,
        DateTimeOffset? ConnectedAt,
        DateTimeOffset? LastSyncedAt,
        string? FolderName,
        string? FolderId,
        DateOnly? InitialImportCutoffDate,
        int? SyncFrequencyMinutes,
        bool AutoAcceptTags,
        bool IsConfigured,
        // Sync tracking fields
        DateTimeOffset? LastSyncAt,
        DateTimeOffset? NextSyncAt,
        int LastSyncFilesDiscovered,
        int LastSyncFilesImported,
        int LastSyncFilesPendingReview,
        int LastSyncFilesErrored,
        string? LastSyncError,
        bool IsSyncPaused,
        int PendingReviewCount);

    public async Task<Result> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var connection = await repository.GetByUserIdAsync(query.UserId, query.ProfileId, cancellationToken);

        if (connection is null)
            return new Result(false, null, null, null, null, null, null, null, false, false,
                null, null, 0, 0, 0, 0, null, false, 0);

        var pendingReviewCount = await fileImportRepository.GetPendingCountByConnectionAsync(
            connection.Id, cancellationToken);

        var nextSyncAt = connection.LastSyncAt.HasValue && connection.SyncFrequencyMinutes > 0
            ? connection.LastSyncAt.Value.AddMinutes(connection.SyncFrequencyMinutes)
            : (DateTimeOffset?)null;

        return new Result(
            true,
            connection.Provider,
            connection.ConnectedAt,
            connection.LastSyncedAt,
            connection.FolderName,
            connection.FolderId,
            connection.InitialImportCutoffDate,
            connection.SyncFrequencyMinutes,
            connection.AutoAcceptTags,
            connection.FolderId is not null,
            // Sync tracking
            connection.LastSyncAt,
            nextSyncAt,
            connection.LastSyncFilesDiscovered,
            connection.LastSyncFilesImported,
            connection.LastSyncFilesPendingReview,
            connection.LastSyncFilesErrored,
            connection.LastSyncError,
            connection.IsSyncPaused,
            pendingReviewCount);
    }
}
