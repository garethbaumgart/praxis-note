using System.Text.Json;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Tags;

public sealed class GetItemsByTag(
    ITagRepository tagRepository,
    INoteRepository noteRepository,
    IMeetingRepository meetingRepository,
    ITaskRepository taskRepository)
{
    public record Query(Guid UserId, Guid TagId);

    public const string NotFoundError = "TAG_NOT_FOUND";

    public async Task<TagItemsResponse> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var tag = await tagRepository.GetByIdAsync(query.TagId, cancellationToken);
        if (tag is null || tag.UserId != query.UserId)
            throw new InvalidOperationException(NotFoundError);

        var notesTask = noteRepository.GetByTagIdAsync(query.UserId, query.TagId, cancellationToken);
        var meetingsTask = meetingRepository.GetByTagIdAsync(query.UserId, query.TagId, cancellationToken);
        var tasksTask = taskRepository.GetTasksWithTagAsync(query.UserId, query.TagId, cancellationToken);

        await Task.WhenAll(notesTask, meetingsTask, tasksTask);

        var notes = notesTask.Result;
        var meetings = meetingsTask.Result;
        var tasks = tasksTask.Result;

        var items = new List<TagItemDto>();

        foreach (var meeting in meetings)
        {
            var attendeeCount = CountAttendees(meeting.Attendees);
            items.Add(new TagItemDto(
                meeting.Id,
                meeting.Title ?? "Untitled Meeting",
                "Meeting",
                meeting.MeetingDate ?? meeting.CreatedAt,
                meeting.MeetingDate,
                attendeeCount,
                null,
                null,
                null,
                null));
        }

        foreach (var note in notes)
        {
            items.Add(new TagItemDto(
                note.Id,
                ExtractNoteTitle(note.Content),
                "Note",
                note.UpdatedAt,
                null,
                null,
                note.UpdatedAt,
                null,
                null,
                null));
        }

        foreach (var task in tasks)
        {
            items.Add(new TagItemDto(
                task.Id,
                task.Title,
                "Task",
                task.CreatedAt,
                null,
                null,
                null,
                task.Status.ToString(),
                task.IsPriority,
                task.DueDate?.Date));
        }

        var sorted = items.OrderByDescending(i => i.Date).ToList();

        return new TagItemsResponse(
            sorted,
            meetings.Count,
            notes.Count,
            tasks.Count,
            sorted.Count);
    }

    private static int CountAttendees(string? attendees)
    {
        if (string.IsNullOrWhiteSpace(attendees))
            return 0;

        return attendees.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
    }

    private static string ExtractNoteTitle(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "Untitled";

        try
        {
            using var doc = JsonDocument.Parse(content);
            var title = FindFirstTitle(doc.RootElement);
            if (!string.IsNullOrWhiteSpace(title))
                return title.Length > 50 ? title[..47] + "..." : title;
        }
        catch (JsonException)
        {
            var firstLine = content.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(firstLine))
                return firstLine.Length > 50 ? firstLine[..47] + "..." : firstLine;
        }

        return "Untitled";
    }

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

public record TagItemDto(
    Guid Id,
    string Title,
    string Type,
    DateTimeOffset Date,
    DateTimeOffset? MeetingDate,
    int? AttendeeCount,
    DateTimeOffset? UpdatedAt,
    string? Status,
    bool? IsPriority,
    DateOnly? DueDate);

public record TagItemsResponse(
    IReadOnlyList<TagItemDto> Items,
    int MeetingCount,
    int NoteCount,
    int TaskCount,
    int TotalCount);
