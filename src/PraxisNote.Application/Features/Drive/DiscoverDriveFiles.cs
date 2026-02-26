using Microsoft.Extensions.Logging;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Drive.Services;
using PraxisNote.Domain.Aggregates.DriveConnections;
using PraxisNote.Domain.Aggregates.DriveFileImports;

namespace PraxisNote.Application.Features.Drive;

public sealed class DiscoverDriveFiles(
    IDriveConnectionRepository connectionRepository,
    IDriveFileImportRepository fileImportRepository,
    IDriveService driveService,
    IUnitOfWork unitOfWork,
    ILogger<DiscoverDriveFiles> logger)
{
    public record Command(Guid UserId, Guid ProfileId);
    public record Result(int NewFilesDiscovered, int AlreadyTracked, int TotalInFolder);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var connection = await connectionRepository.GetByUserIdAsync(command.UserId, command.ProfileId, cancellationToken)
            ?? throw new InvalidOperationException("No Google Drive connection found.");

        if (string.IsNullOrEmpty(connection.FolderId))
            throw new InvalidOperationException("No folder configured. Please select a folder first.");

        // Refresh token if expired
        if (connection.IsTokenExpired())
        {
            var refreshResult = await driveService.RefreshAccessTokenAsync(connection.RefreshToken, cancellationToken);
            connection.UpdateTokens(refreshResult.AccessToken, refreshResult.ExpiresAt, refreshResult.RefreshToken);
        }

        // Determine cutoff: first run uses InitialImportCutoffDate, subsequent uses LastSyncedAt
        var modifiedAfter = connection.LastSyncedAt ??
            (connection.InitialImportCutoffDate.HasValue
                ? new DateTimeOffset(connection.InitialImportCutoffDate.Value, TimeOnly.MinValue, TimeSpan.Zero)
                : null);

        // Page through all files in the folder
        var allFiles = new List<DriveFile>();
        string? pageToken = null;
        do
        {
            var page = await driveService.ListFilesAsync(
                connection.AccessToken, connection.FolderId, modifiedAfter, pageToken, cancellationToken);
            allFiles.AddRange(page.Files);
            pageToken = page.NextPageToken;
        } while (!string.IsNullOrEmpty(pageToken));

        if (allFiles.Count == 0)
        {
            connection.RecordSync();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new Result(0, 0, 0);
        }

        // Check which files are already tracked
        var driveFileIds = allFiles.Select(f => f.Id).ToList();
        var existingIds = await fileImportRepository.GetExistingDriveFileIdsAsync(connection.Id, driveFileIds, cancellationToken);

        var newFiles = new List<DriveFileImport>();
        foreach (var file in allFiles)
        {
            if (existingIds.Contains(file.Id))
                continue;

            newFiles.Add(DriveFileImport.Create(
                connection.Id,
                file.Id,
                file.Name,
                file.MimeType,
                file.ModifiedTime ?? DateTimeOffset.UtcNow));
        }

        if (newFiles.Count > 0)
        {
            await fileImportRepository.AddRangeAsync(newFiles, cancellationToken);
        }

        connection.RecordSync();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Drive discovery: {New} new files, {Existing} already tracked, {Total} total in folder",
            newFiles.Count, existingIds.Count, allFiles.Count);

        return new Result(newFiles.Count, existingIds.Count, allFiles.Count);
    }
}
