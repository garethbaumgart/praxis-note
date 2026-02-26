using System.Text.Json;
using Microsoft.Extensions.Logging;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Drive.Services;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Domain.Aggregates.DriveConnections;
using PraxisNote.Domain.Aggregates.DriveFileImports;
using PraxisNote.Domain.Aggregates.Users;

namespace PraxisNote.Application.Features.Drive;

public sealed class DriveSyncOrchestrator(
    IDriveConnectionRepository driveConnectionRepository,
    IDriveFileImportRepository driveFileImportRepository,
    IDriveService driveService,
    ParseTranscriptForImport parseTranscript,
    ITranscriptExtractor transcriptExtractor,
    IDriveDeduplicationService deduplicationService,
    ConfirmDriveImport confirmDriveImport,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ILogger<DriveSyncOrchestrator> logger)
{
    private const int MaxFilesPerSync = 50;
    private const int ErrorMessageMaxLength = 2000;
    private const string GoogleDocMimeType = "application/vnd.google-apps.document";
    private const string DocxMimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private const string PlainTextMimeType = "text/plain";

    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    internal static readonly TimeSpan DelayBetweenCalls = TimeSpan.FromSeconds(1);

    public record SyncResult(
        int FilesDiscovered,
        int FilesImported,
        int FilesPendingReview,
        int FilesErrored,
        string? Error);

    /// <summary>Run a full sync cycle for a single DriveConnection.</summary>
    public async Task<SyncResult> SyncConnectionAsync(
        Guid connectionId,
        CancellationToken ct = default)
    {
        var connection = await driveConnectionRepository.GetByIdAsync(connectionId, ct);
        if (connection is null)
        {
            logger.LogWarning("Drive connection {ConnectionId} not found for sync", connectionId);
            return new SyncResult(0, 0, 0, 0, "Connection not found");
        }

        return await RunSyncCycleAsync(connection, ct);
    }

    /// <summary>Run a manual sync triggered by user (same logic, clears error state first).</summary>
    public async Task<SyncResult> ManualSyncAsync(
        Guid userId,
        Guid profileId,
        Guid connectionId,
        CancellationToken ct = default)
    {
        var connection = await driveConnectionRepository.GetByIdAsync(connectionId, ct);
        if (connection is null || connection.UserId != userId)
        {
            return new SyncResult(0, 0, 0, 0, "Connection not found");
        }

        connection.ClearSyncError();
        await unitOfWork.SaveChangesAsync(ct);

        return await RunSyncCycleAsync(connection, ct);
    }

    private async Task<SyncResult> RunSyncCycleAsync(DriveConnection connection, CancellationToken ct)
    {
        try
        {
            // 1. Refresh token if expired
            if (connection.IsTokenExpired())
            {
                try
                {
                    var refreshResult = await driveService.RefreshAccessTokenAsync(connection.RefreshToken, ct);
                    connection.UpdateTokens(refreshResult.AccessToken, refreshResult.ExpiresAt, refreshResult.RefreshToken);
                    await unitOfWork.SaveChangesAsync(ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Token refresh failed for connection {ConnectionId}", connection.Id);
                    connection.RecordSyncFailure("OAuth token expired. Please reconnect Google Drive.");
                    await unitOfWork.SaveChangesAsync(ct);
                    return new SyncResult(0, 0, 0, 0, "OAuth token expired. Please reconnect Google Drive.");
                }
            }

            if (string.IsNullOrEmpty(connection.FolderId))
            {
                connection.RecordSyncFailure("No folder configured.");
                await unitOfWork.SaveChangesAsync(ct);
                return new SyncResult(0, 0, 0, 0, "No folder configured.");
            }

            // 2. Discover files
            var modifiedAfter = connection.LastSyncedAt ??
                (connection.InitialImportCutoffDate.HasValue
                    ? new DateTimeOffset(connection.InitialImportCutoffDate.Value, TimeOnly.MinValue, TimeSpan.Zero)
                    : null);

            List<DriveFile> allFiles;
            try
            {
                allFiles = [];
                string? pageToken = null;
                do
                {
                    var page = await driveService.ListFilesAsync(
                        connection.AccessToken, connection.FolderId, modifiedAfter, pageToken, ct);
                    allFiles.AddRange(page.Files);
                    pageToken = page.NextPageToken;
                } while (!string.IsNullOrEmpty(pageToken));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Folder listing failed for connection {ConnectionId}", connection.Id);
                connection.RecordSyncFailure("Configured folder no longer exists or is inaccessible.");
                await unitOfWork.SaveChangesAsync(ct);
                return new SyncResult(0, 0, 0, 0, "Configured folder no longer exists or is inaccessible.");
            }

            // 3. Filter out already-tracked files
            var driveFileIds = allFiles.Select(f => f.Id).ToList();
            var existingIds = driveFileIds.Count > 0
                ? await driveFileImportRepository.GetExistingDriveFileIdsAsync(connection.Id, driveFileIds, ct)
                : [];

            var newFiles = allFiles
                .Where(f => !existingIds.Contains(f.Id))
                .Take(MaxFilesPerSync)
                .ToList();

            if (newFiles.Count == 0)
            {
                connection.RecordSync();
                connection.RecordSyncResult(0, 0, 0, 0);
                await unitOfWork.SaveChangesAsync(ct);
                return new SyncResult(0, 0, 0, 0, null);
            }

            // Get user info for parsing
            var user = await userRepository.GetByIdAsync(connection.UserId, ct);
            var userName = user?.Name;
            var timeZone = connection.TimeZone;

            // 4. Download, parse, and create DriveFileImport records per file
            var newImports = new List<DriveFileImport>();
            var errorCount = 0;
            var hasIssuedAiCall = false;

            foreach (var file in newFiles)
            {
                var import = DriveFileImport.Create(
                    connection.Id,
                    file.Id,
                    file.Name,
                    file.MimeType,
                    file.ModifiedTime ?? DateTimeOffset.UtcNow);

                try
                {
                    // Download/export file content
                    var text = await ExtractTextFromDriveFileAsync(
                        connection.AccessToken, import.DriveFileId, import.MimeType, ct);

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        import.MarkSkipped("File is empty or contains no extractable text");
                        await driveFileImportRepository.AddAsync(import, ct);
                        await unitOfWork.SaveChangesAsync(ct);
                        continue;
                    }

                    // Rate limiting
                    if (hasIssuedAiCall)
                    {
                        await Task.Delay(DelayBetweenCalls, ct);
                    }

                    // Parse via AI
                    var parseCommand = new ParseTranscriptForImport.Command(
                        connection.UserId, userName, timeZone, text, null, null, import.FileName);

                    var parseResult = await parseTranscript.ExecuteAsync(parseCommand, ct);
                    hasIssuedAiCall = true;

                    var resultJson = JsonSerializer.Serialize(parseResult, CamelCaseOptions);
                    import.MarkParsed(text, resultJson);
                    newImports.Add(import);

                    logger.LogInformation("Background sync parsed Drive file {FileName} ({FileId})",
                        import.FileName, import.DriveFileId);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    var errorMessage = ex.Message.Length > ErrorMessageMaxLength
                        ? ex.Message[..ErrorMessageMaxLength]
                        : ex.Message;
                    import.MarkError(errorMessage);
                    errorCount++;
                    logger.LogWarning(ex, "Background sync failed to parse Drive file {FileName} ({FileId})",
                        import.FileName, import.DriveFileId);
                }

                await driveFileImportRepository.AddAsync(import, ct);
                await unitOfWork.SaveChangesAsync(ct);

                if (ct.IsCancellationRequested) break;
            }

            // 5. Run deduplication on newly parsed files
            if (newImports.Count > 0)
            {
                await deduplicationService.DeduplicateAsync(
                    connection.UserId, connection.ProfileId, newImports, ct);
                await unitOfWork.SaveChangesAsync(ct);
            }

            // 6. Auto-accept or queue for review
            var importedCount = 0;
            var pendingReviewCount = 0;

            if (connection.AutoAcceptTags && newImports.Count > 0)
            {
                // Auto-accept: import non-duplicates
                var nonDuplicates = newImports
                    .Where(f => f.DuplicateType == DeduplicationType.None && f.Status == DriveFileImportStatus.Parsed)
                    .ToList();

                var duplicates = newImports
                    .Where(f => f.DuplicateType != DeduplicationType.None && f.Status == DriveFileImportStatus.Parsed)
                    .ToList();

                if (nonDuplicates.Count > 0)
                {
                    try
                    {
                        var selectedFiles = nonDuplicates.Select(f =>
                        {
                            // Extract suggested tags from parsed result
                            var tags = ExtractSuggestedTags(f.ParsedResultJson);
                            return new ConfirmDriveImport.SelectedFile(f.Id, tags);
                        }).ToList();

                        var importCommand = new ConfirmDriveImport.Command(
                            connection.UserId, connection.ProfileId, selectedFiles);
                        var importResult = await confirmDriveImport.ExecuteAsync(importCommand, ct);
                        importedCount = importResult.ImportedCount;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        logger.LogWarning(ex, "Auto-import failed for connection {ConnectionId}", connection.Id);
                        // Fall through — files remain in Parsed state for manual review
                    }
                }

                pendingReviewCount = duplicates.Count + (nonDuplicates.Count - importedCount);
            }
            else
            {
                // Review path: all parsed files are pending review
                pendingReviewCount = newImports.Count(f => f.Status == DriveFileImportStatus.Parsed);
            }

            // 7. Record sync result
            connection.RecordSync();
            connection.RecordSyncResult(newFiles.Count, importedCount, pendingReviewCount, errorCount);
            await unitOfWork.SaveChangesAsync(ct);

            return new SyncResult(newFiles.Count, importedCount, pendingReviewCount, errorCount, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during sync for connection {ConnectionId}", connection.Id);
            try
            {
                connection.RecordSyncFailure(ex.Message.Length > 500 ? ex.Message[..500] : ex.Message);
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch
            {
                // Best-effort save
            }
            return new SyncResult(0, 0, 0, 0, ex.Message);
        }
    }

    private async Task<string> ExtractTextFromDriveFileAsync(
        string accessToken, string driveFileId, string mimeType, CancellationToken ct)
    {
        return mimeType switch
        {
            GoogleDocMimeType => await driveService.ExportGoogleDocAsync(accessToken, driveFileId, ct),
            DocxMimeType => await ExtractDocxTextAsync(accessToken, driveFileId, ct),
            PlainTextMimeType => await ExtractPlainTextAsync(accessToken, driveFileId, ct),
            _ => throw new InvalidOperationException($"Unsupported mime type: {mimeType}")
        };
    }

    private async Task<string> ExtractDocxTextAsync(string accessToken, string driveFileId, CancellationToken ct)
    {
        await using var stream = await driveService.DownloadFileAsync(accessToken, driveFileId, ct);
        return await transcriptExtractor.ExtractTextFromDocxAsync(stream, ct);
    }

    private async Task<string> ExtractPlainTextAsync(string accessToken, string driveFileId, CancellationToken ct)
    {
        await using var stream = await driveService.DownloadFileAsync(accessToken, driveFileId, ct);
        return transcriptExtractor.ExtractTextFromPlainText(stream);
    }

    private static List<string> ExtractSuggestedTags(string? parsedResultJson)
    {
        if (string.IsNullOrWhiteSpace(parsedResultJson)) return [];

        try
        {
            using var doc = JsonDocument.Parse(parsedResultJson);
            if (doc.RootElement.TryGetProperty("suggestedTags", out var tagsElement) &&
                tagsElement.ValueKind == JsonValueKind.Array)
            {
                return tagsElement.EnumerateArray()
                    .Select(t => t.GetString())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => t!)
                    .ToList();
            }
        }
        catch
        {
            // Best-effort tag extraction
        }

        return [];
    }
}
