using System.Text.Json;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Domain.Aggregates.DriveConnections;
using PraxisNote.Domain.Aggregates.DriveFileImports;

namespace PraxisNote.Application.Features.Drive;

public sealed class ConfirmDriveImport(
    ConfirmTranscriptImport confirmTranscriptImport,
    IDriveFileImportRepository driveFileImportRepository,
    IDriveConnectionRepository connectionRepository)
{
    public record SelectedFile(
        Guid DriveFileImportId,
        List<string> Tags);

    public record Command(Guid UserId, Guid ProfileId, List<SelectedFile> Files);

    public record Result(int ImportedCount, int TotalActionItems, int TagsCreated, int SkippedCount, List<FailedImport> Failures);

    public record FailedImport(Guid DriveFileImportId, string FileName, string Error);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken ct = default)
    {
        if (command.Files.Count == 0)
        {
            return new Result(0, 0, 0, 0, []);
        }

        // Verify ownership via DriveConnection
        var connection = await connectionRepository.GetByUserIdAsync(command.UserId, command.ProfileId, ct)
            ?? throw new InvalidOperationException("No Google Drive connection found.");

        // Build lookup for user-edited tags (deduplicate by ID, last wins)
        var tagsByFileId = command.Files
            .GroupBy(f => f.DriveFileImportId)
            .ToDictionary(g => g.Key, g => g.Last().Tags);

        // Load all DriveFileImport entities
        var driveImports = new List<DriveFileImport>();
        foreach (var fileId in tagsByFileId.Keys)
        {
            var import = await driveFileImportRepository.GetByIdAsync(fileId, ct);
            if (import is not null && import.DriveConnectionId == connection.Id)
                driveImports.Add(import);
        }

        var skippedCount = 0;
        var importItems = new List<ConfirmTranscriptImport.ImportItem>();
        var failures = new List<FailedImport>();

        foreach (var driveImport in driveImports)
        {
            // Only Parsed files are eligible for import
            if (driveImport.Status != DriveFileImportStatus.Parsed)
            {
                skippedCount++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(driveImport.ParsedResultJson))
            {
                failures.Add(new FailedImport(driveImport.Id, driveImport.FileName, "No parsed result available"));
                continue;
            }

            try
            {
                var parsed = JsonSerializer.Deserialize<ParsedResult>(
                    driveImport.ParsedResultJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (parsed is null)
                {
                    failures.Add(new FailedImport(driveImport.Id, driveImport.FileName, "Failed to deserialize parsed result"));
                    continue;
                }

                var userTags = tagsByFileId.TryGetValue(driveImport.Id, out var tags) ? tags : [];

                DateTimeOffset? meetingDate = null;
                if (!string.IsNullOrWhiteSpace(parsed.MeetingDate) &&
                    DateTimeOffset.TryParse(parsed.MeetingDate, out var parsedDate))
                {
                    meetingDate = parsedDate;
                }

                var actionItems = parsed.ActionItems?
                    .Select(a => new ConfirmTranscriptImport.ActionItemInput(a.Description, a.Assignee))
                    .ToList() ?? [];

                var transcript = parsed.Transcript ?? driveImport.ParsedContent ?? "";
                if (string.IsNullOrWhiteSpace(transcript))
                {
                    failures.Add(new FailedImport(driveImport.Id, driveImport.FileName, "No transcript content available"));
                    continue;
                }

                importItems.Add(new ConfirmTranscriptImport.ImportItem(
                    parsed.Title,
                    meetingDate,
                    parsed.Attendees,
                    transcript,
                    parsed.Summary,
                    parsed.KeyPoints is not null ? string.Join("\n", parsed.KeyPoints) : null,
                    parsed.Decisions is not null ? string.Join("\n", parsed.Decisions) : null,
                    actionItems,
                    userTags,
                    driveImport.Id));
            }
            catch (Exception)
            {
                failures.Add(new FailedImport(driveImport.Id, driveImport.FileName, "Failed to process file for import"));
            }
        }

        // Import files one at a time for per-file failure isolation
        var totalImported = 0;
        var totalActionItems = 0;
        var totalTagsCreated = 0;

        foreach (var item in importItems)
        {
            try
            {
                var singleCommand = new ConfirmTranscriptImport.Command(command.UserId, command.ProfileId, [item]);
                var singleResult = await confirmTranscriptImport.ExecuteAsync(singleCommand, ct);
                totalImported += singleResult.ImportedCount;
                totalActionItems += singleResult.TotalActionItems;
                totalTagsCreated += singleResult.TagsCreated;
            }
            catch (Exception)
            {
                var fileName = driveImports.FirstOrDefault(d => d.Id == item.DriveFileImportId)?.FileName ?? "Unknown";
                failures.Add(new FailedImport(item.DriveFileImportId ?? Guid.Empty, fileName, "Failed to import meeting"));
            }
        }

        return new Result(
            totalImported,
            totalActionItems,
            totalTagsCreated,
            skippedCount,
            failures);
    }

    /// <summary>
    /// Internal DTO for deserializing ParsedResultJson (matches ParseTranscriptForImport.Result shape).
    /// </summary>
    private sealed class ParsedResult
    {
        public string? Title { get; set; }
        public string? MeetingDate { get; set; }
        public string? Attendees { get; set; }
        public string? Summary { get; set; }
        public List<string>? KeyPoints { get; set; }
        public List<string>? Decisions { get; set; }
        public List<ParsedActionItem>? ActionItems { get; set; }
        public List<string>? SuggestedTags { get; set; }
        public string? Transcript { get; set; }
    }

    private sealed class ParsedActionItem
    {
        public string Description { get; set; } = "";
        public string? Assignee { get; set; }
    }
}
