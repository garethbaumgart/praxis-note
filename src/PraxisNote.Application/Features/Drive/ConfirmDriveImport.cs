using System.Text.Json;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Domain.Aggregates.DriveFileImports;

namespace PraxisNote.Application.Features.Drive;

public sealed class ConfirmDriveImport(
    ConfirmTranscriptImport confirmTranscriptImport,
    IDriveFileImportRepository driveFileImportRepository)
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

        // Load all DriveFileImport entities
        var driveImports = new List<DriveFileImport>();
        foreach (var file in command.Files)
        {
            var import = await driveFileImportRepository.GetByIdAsync(file.DriveFileImportId, ct);
            if (import is not null)
                driveImports.Add(import);
        }

        // Build lookup for user-edited tags
        var tagsByFileId = command.Files.ToDictionary(f => f.DriveFileImportId, f => f.Tags);

        var skippedCount = 0;
        var importItems = new List<ConfirmTranscriptImport.ImportItem>();
        var failures = new List<FailedImport>();

        foreach (var driveImport in driveImports)
        {
            // Skip already-imported files
            if (driveImport.Status == DriveFileImportStatus.Imported)
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

                importItems.Add(new ConfirmTranscriptImport.ImportItem(
                    parsed.Title,
                    meetingDate,
                    parsed.Attendees,
                    parsed.Transcript ?? driveImport.ParsedContent ?? "",
                    parsed.Summary,
                    parsed.KeyPoints is not null ? string.Join("\n", parsed.KeyPoints) : null,
                    parsed.Decisions is not null ? string.Join("\n", parsed.Decisions) : null,
                    actionItems,
                    userTags,
                    driveImport.Id));
            }
            catch (Exception ex)
            {
                failures.Add(new FailedImport(driveImport.Id, driveImport.FileName, ex.Message));
            }
        }

        var importResult = new ConfirmTranscriptImport.Result(0, 0, 0);

        if (importItems.Count > 0)
        {
            var importCommand = new ConfirmTranscriptImport.Command(command.UserId, command.ProfileId, importItems);
            importResult = await confirmTranscriptImport.ExecuteAsync(importCommand, ct);
        }

        return new Result(
            importResult.ImportedCount,
            importResult.TotalActionItems,
            importResult.TagsCreated,
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
