using System.Text.Json;
using Microsoft.Extensions.Options;
using PraxisNote.Application.Features.Tags;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;
using TaskStatus = PraxisNote.Domain.ValueObjects.TaskStatus;

namespace PraxisNote.Application.Features.Tasks;

public sealed class GetUserTasks(
    ITaskRepository taskRepository,
    ITagRepository tagRepository,
    IMeetingRepository meetingRepository,
    INoteRepository noteRepository,
    IOptions<TaskSettings> settings)
{
    private readonly TaskSettings _settings = settings.Value;

    public record Query(Guid UserId, bool IncludeArchived = false);

    public async Task<IReadOnlyList<TaskDto>> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var tasks = await taskRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        var tags = await tagRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        var tagLookup = tags.ToDictionary(t => t.Id);

        // Build source title lookups for linked tasks
        var sourceLookup = await BuildSourceLookupAsync(tasks, query.UserId, cancellationToken);

        var archiveThreshold = DateTimeOffset.UtcNow.AddDays(-_settings.ArchiveThresholdDays);

        var filteredTasks = query.IncludeArchived
            ? tasks
                .Where(t => t.Status == TaskStatus.Done
                    && t.CompletedAt.HasValue
                    && t.CompletedAt.Value < archiveThreshold)
                .OrderByDescending(t => t.CompletedAt)
                .Take(_settings.MaxArchivedTasks)
            : tasks
                .Where(t => t.Status != TaskStatus.Done
                    || !t.CompletedAt.HasValue
                    || t.CompletedAt.Value >= archiveThreshold)
                .OrderBy(t => t.Position);

        return filteredTasks
            .Select(t => new TaskDto(
                t.Id,
                t.Title,
                t.Status.ToString(),
                t.Position,
                t.IsPriority,
                t.CreatedAt,
                t.StartedAt,
                t.CompletedAt,
                t.Comments
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new CommentDto(c.Id, c.Content, c.CreatedAt, c.UpdatedAt))
                    .ToList(),
                t.DueDate?.Date,
                t.TagIds
                    .Where(id => tagLookup.ContainsKey(id))
                    .Select(id => new TaskTagDto(id, tagLookup[id].Name))
                    .ToList(),
                sourceLookup.GetValueOrDefault(t.Id)))
            .ToList();
    }

    /// <summary>
    /// Builds a lookup of task ID → source DTO for all tasks linked to meetings or notes.
    /// Fetches meetings/notes in bulk to avoid N+1 queries.
    /// </summary>
    private async Task<Dictionary<Guid, TaskSourceDto>> BuildSourceLookupAsync(
        IReadOnlyList<TaskItem> tasks,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, TaskSourceDto>();

        var meetingLinkedTasks = tasks.Where(t => t.IsLinkedToMeeting).ToList();
        var noteLinkedTasks = tasks.Where(t => t.IsLinkedToNote).ToList();

        if (meetingLinkedTasks.Count == 0 && noteLinkedTasks.Count == 0)
            return result;

        // Fetch all user meetings/notes in one query each (already loaded by user)
        if (meetingLinkedTasks.Count > 0)
        {
            var meetings = await meetingRepository.GetByUserIdAsync(userId, cancellationToken);
            var meetingLookup = meetings.ToDictionary(m => m.Id);

            foreach (var task in meetingLinkedTasks)
            {
                var meetingId = task.ActionItemRef!.MeetingId;
                if (meetingLookup.TryGetValue(meetingId, out var meeting))
                {
                    var title = meeting.Title ?? "Untitled Meeting";
                    result[task.Id] = new TaskSourceDto("meeting", meetingId, title);
                }
            }
        }

        if (noteLinkedTasks.Count > 0)
        {
            var notes = await noteRepository.GetByUserIdAsync(userId, cancellationToken);
            var noteLookup = notes.ToDictionary(n => n.Id);

            foreach (var task in noteLinkedTasks)
            {
                var noteId = task.CheckboxRef!.NoteId;
                if (noteLookup.TryGetValue(noteId, out var note))
                {
                    var title = ExtractNoteTitle(note.Content);
                    result[task.Id] = new TaskSourceDto("note", noteId, title);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Extracts a display title from TipTap JSON content.
    /// Returns the first heading text, or the first paragraph text, or "Untitled Note".
    /// </summary>
    private static string ExtractNoteTitle(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "Untitled Note";

        try
        {
            using var doc = JsonDocument.Parse(content);
            var title = FindFirstTitle(doc.RootElement);
            if (!string.IsNullOrWhiteSpace(title))
                return title.Length > 60 ? title[..57] + "..." : title;
        }
        catch (JsonException)
        {
            // Content is not valid JSON — try plain text fallback
            var firstLine = content.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(firstLine))
                return firstLine.Length > 60 ? firstLine[..57] + "..." : firstLine;
        }

        return "Untitled Note";
    }

    /// <summary>
    /// Recursively searches a TipTap JSON tree for the first heading or paragraph with text.
    /// Handles nested structures like taskList, bulletList, and listItem.
    /// </summary>
    private static string? FindFirstTitle(JsonElement node)
    {
        if (node.ValueKind != JsonValueKind.Object)
            return null;

        if (node.TryGetProperty("type", out var typeElement))
        {
            var type = typeElement.GetString();
            if (type is "heading" or "paragraph")
            {
                var text = ExtractTextFromNode(node);
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }
        }

        if (node.TryGetProperty("content", out var contentArray) &&
            contentArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in contentArray.EnumerateArray())
            {
                var found = FindFirstTitle(child);
                if (!string.IsNullOrWhiteSpace(found))
                    return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts text content from a TipTap node's immediate text children.
    /// </summary>
    private static string ExtractTextFromNode(JsonElement node)
    {
        if (!node.TryGetProperty("content", out var content) ||
            content.ValueKind != JsonValueKind.Array)
            return string.Empty;

        var parts = content.EnumerateArray()
            .Where(child =>
                child.TryGetProperty("type", out var childType) &&
                childType.GetString() == "text" &&
                child.TryGetProperty("text", out _))
            .Select(child => child.GetProperty("text").GetString() ?? string.Empty);

        return string.Join("", parts);
    }
}
