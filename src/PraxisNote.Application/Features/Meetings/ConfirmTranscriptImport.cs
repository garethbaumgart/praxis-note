using System.Text.Json;
using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.DriveFileImports;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Tags;

namespace PraxisNote.Application.Features.Meetings;

public sealed class ConfirmTranscriptImport(
    IMeetingRepository meetingRepository,
    ITagRepository tagRepository,
    IUnitOfWork unitOfWork,
    IDriveFileImportRepository? driveFileImportRepository = null)
{
    public record ImportItem(
        string? Title,
        DateTimeOffset? MeetingDate,
        string? Attendees,
        string Transcript,
        string? Summary,
        string? KeyPoints,
        string? Decisions,
        List<ActionItemInput> ActionItems,
        List<string> SuggestedTags,
        Guid? DriveFileImportId = null);

    public record ActionItemInput(string Description, string? Assignee);

    public record Command(Guid UserId, Guid ProfileId, List<ImportItem> Meetings);
    public record Result(int ImportedCount, int TotalActionItems, int TagsCreated);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        if (command.Meetings.Count == 0)
        {
            return new Result(0, 0, 0);
        }

        // Collect all suggested tag names across all meetings
        var allTagNames = command.Meetings
            .SelectMany(m => m.SuggestedTags)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Build tag lookup map: lowercase name -> tag ID
        var tagMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        if (allTagNames.Count > 0)
        {
            var existingTags = await tagRepository.GetByNamesAsync(
                command.UserId, command.ProfileId, allTagNames, cancellationToken);
            foreach (var tag in existingTags)
            {
                tagMap[tag.Name] = tag.Id;
            }
        }

        // Create missing tags
        var tagsCreated = 0;
        foreach (var name in allTagNames)
        {
            if (string.IsNullOrWhiteSpace(name) || tagMap.ContainsKey(name))
                continue;

            var newTag = Tag.Create(command.UserId, command.ProfileId, name);
            await tagRepository.AddAsync(newTag, cancellationToken);
            tagMap[name] = newTag.Id;
            tagsCreated++;
        }

        var totalActionItems = 0;

        foreach (var item in command.Meetings)
        {
            // 1. Create meeting
            var meeting = Meeting.Create(
                command.UserId,
                command.ProfileId,
                item.Title,
                item.MeetingDate,
                item.Attendees);

            // 2. Submit transcript
            meeting.SubmitTranscript(item.Transcript);

            // 3. Convert action items to domain objects
            var actionItems = item.ActionItems
                .Where(a => !string.IsNullOrWhiteSpace(a.Description))
                .Select(a => ActionItem.Create(a.Description, a.Assignee))
                .ToList();

            totalActionItems += actionItems.Count;

            // 4. Complete analysis with pre-parsed AI results
            var summary = string.IsNullOrWhiteSpace(item.Summary)
                ? "Imported meeting"
                : item.Summary;
            meeting.CompleteAnalysis(
                summary,
                item.KeyPoints,
                item.Decisions,
                actionItems,
                suggestedTitle: null,
                item.SuggestedTags.Count > 0
                    ? JsonSerializer.Serialize(item.SuggestedTags)
                    : null);

            // 5. Add matching tags
            foreach (var tagName in item.SuggestedTags)
            {
                if (tagMap.TryGetValue(tagName, out var tagId))
                {
                    meeting.AddTag(tagId);
                }
            }

            await meetingRepository.AddAsync(meeting, cancellationToken);

            // Mark Drive file import as imported if applicable
            if (item.DriveFileImportId.HasValue && driveFileImportRepository is not null)
            {
                var driveImport = await driveFileImportRepository.GetByIdAsync(
                    item.DriveFileImportId.Value, cancellationToken);
                if (driveImport?.Status == DriveFileImportStatus.Parsed)
                {
                    driveImport.MarkImported(meeting.Id);
                }
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new Result(command.Meetings.Count, totalActionItems, tagsCreated);
    }
}
