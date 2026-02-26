using Microsoft.Extensions.Logging;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Drive.Services;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Domain.Aggregates.DriveConnections;
using PraxisNote.Domain.Aggregates.DriveFileImports;
using PraxisNote.Domain.Aggregates.Users;

namespace PraxisNote.Application.Features.Drive;

public sealed class ParseDriveFiles(
    IDriveConnectionRepository connectionRepository,
    IDriveFileImportRepository fileImportRepository,
    IDriveService driveService,
    ParseTranscriptForImport parseTranscript,
    ITranscriptExtractor transcriptExtractor,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ILogger<ParseDriveFiles> logger)
{
    private const int MaxFilesPerCycle = 50;
    internal static readonly TimeSpan DelayBetweenCalls = TimeSpan.FromSeconds(1);

    private const string GoogleDocMimeType = "application/vnd.google-apps.document";
    private const string DocxMimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private const string PlainTextMimeType = "text/plain";

    public record Command(Guid UserId, Guid ProfileId);
    public record Result(int Parsed, int Errors, int Remaining);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var connection = await connectionRepository.GetByUserIdAsync(command.UserId, command.ProfileId, cancellationToken)
            ?? throw new InvalidOperationException("No Google Drive connection found.");

        // Refresh token if expired
        if (connection.IsTokenExpired())
        {
            var refreshResult = await driveService.RefreshAccessTokenAsync(connection.RefreshToken, cancellationToken);
            connection.UpdateTokens(refreshResult.AccessToken, refreshResult.ExpiresAt, refreshResult.RefreshToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Get pending files, limited to batch size
        var pendingFiles = await fileImportRepository.GetByStatusAsync(
            connection.Id, DriveFileImportStatus.Pending, cancellationToken);

        var filesToProcess = pendingFiles.Take(MaxFilesPerCycle).ToList();
        var remaining = pendingFiles.Count - filesToProcess.Count;

        // Get user name for person tag extraction
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        var userName = user?.Name;
        var timeZone = connection.TimeZone;

        var parsed = 0;
        var errors = 0;

        foreach (var fileImport in filesToProcess)
        {
            try
            {
                // 1. Download/export file content
                var text = await ExtractTextFromDriveFileAsync(
                    connection.AccessToken, fileImport.DriveFileId, fileImport.MimeType, cancellationToken);

                if (string.IsNullOrWhiteSpace(text))
                {
                    fileImport.MarkSkipped("File is empty or contains no extractable text");
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                    continue;
                }

                // 2. Parse through existing AI pipeline
                var parseCommand = new ParseTranscriptForImport.Command(
                    command.UserId,
                    userName,
                    timeZone,
                    text,
                    null,
                    null,
                    fileImport.FileName);

                var parseResult = await parseTranscript.ExecuteAsync(parseCommand, cancellationToken);

                // 3. Serialize result as JSON for preview
                var resultJson = System.Text.Json.JsonSerializer.Serialize(parseResult);

                // 4. Update file import with parsed result
                fileImport.MarkParsed(text, resultJson);
                parsed++;

                logger.LogInformation("Parsed Drive file {FileName} ({FileId})", fileImport.FileName, fileImport.DriveFileId);
            }
            catch (Exception ex)
            {
                fileImport.MarkError(ex.Message);
                errors++;
                logger.LogWarning(ex, "Failed to parse Drive file {FileName} ({FileId})", fileImport.FileName, fileImport.DriveFileId);
            }

            // Save after each file to avoid losing progress on failure
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Rate limiting: wait between AI calls
            if (cancellationToken.IsCancellationRequested) break;
            await Task.Delay(DelayBetweenCalls, cancellationToken);
        }

        return new Result(parsed, errors, remaining);
    }

    private async Task<string> ExtractTextFromDriveFileAsync(
        string accessToken, string driveFileId, string mimeType, CancellationToken cancellationToken)
    {
        return mimeType switch
        {
            GoogleDocMimeType => await driveService.ExportGoogleDocAsync(accessToken, driveFileId, cancellationToken),
            DocxMimeType => await ExtractDocxTextAsync(accessToken, driveFileId, cancellationToken),
            PlainTextMimeType => await ExtractPlainTextAsync(accessToken, driveFileId, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported mime type: {mimeType}")
        };
    }

    private async Task<string> ExtractDocxTextAsync(
        string accessToken, string driveFileId, CancellationToken cancellationToken)
    {
        await using var stream = await driveService.DownloadFileAsync(accessToken, driveFileId, cancellationToken);
        return await transcriptExtractor.ExtractTextFromDocxAsync(stream, cancellationToken);
    }

    private async Task<string> ExtractPlainTextAsync(
        string accessToken, string driveFileId, CancellationToken cancellationToken)
    {
        await using var stream = await driveService.DownloadFileAsync(accessToken, driveFileId, cancellationToken);
        return transcriptExtractor.ExtractTextFromPlainText(stream);
    }
}
