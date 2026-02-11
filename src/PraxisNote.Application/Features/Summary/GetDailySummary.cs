using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Summary;

public sealed class GetDailySummary(
    IMeetingRepository meetingRepository,
    ITaskRepository taskRepository,
    INoteRepository noteRepository)
{
    public record Query(Guid UserId, Guid ProfileId, DateOnly Date);

    public async Task<DailySummaryDto> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var date = query.Date;
        var dayStart = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        // Load all data for the user (sequential — EF Core DbContext is not thread-safe)
        var allMeetings = await meetingRepository.GetByUserIdAsync(query.UserId, query.ProfileId, cancellationToken);
        var allTasks = await taskRepository.GetByUserIdAsync(query.UserId, query.ProfileId, cancellationToken);
        var allNotes = await noteRepository.GetByUserIdAsync(query.UserId, query.ProfileId, cancellationToken);

        // Meetings for the target date (by MeetingDate)
        var meetingsToday = allMeetings
            .Where(m =>
            {
                var meetingDate = m.MeetingDate ?? m.CreatedAt;
                return meetingDate >= dayStart && meetingDate < dayEnd;
            })
            .OrderBy(m => m.MeetingDate ?? m.CreatedAt)
            .ToList();

        // Tasks completed on the target date
        var completedTasks = allTasks
            .Where(t => t.CompletedAt.HasValue && t.CompletedAt.Value >= dayStart && t.CompletedAt.Value < dayEnd)
            .OrderByDescending(t => t.CompletedAt)
            .ToList();

        // Tasks started (moved to InProgress) on the target date
        var startedTasks = allTasks
            .Where(t => t.StartedAt.HasValue && t.StartedAt.Value >= dayStart && t.StartedAt.Value < dayEnd
                        && t.Status == Domain.ValueObjects.TaskStatus.InProgress)
            .OrderByDescending(t => t.StartedAt)
            .ToList();

        // Notes updated on the target date
        var notesUpdated = allNotes
            .Where(n => n.UpdatedAt >= dayStart && n.UpdatedAt < dayEnd)
            .OrderByDescending(n => n.UpdatedAt)
            .ToList();

        // Outstanding action items: all uncompleted from meetings in last 30 days
        var actionItemCutoff = dayEnd.AddDays(-30);
        var linkedTaskIds = allTasks
            .Where(t => t.ActionItemRef is not null && t.ActionItemRef.IsLinked)
            .GroupBy(t => t.ActionItemRef!.ActionItemId)
            .ToDictionary(g => g.Key, g =>
            {
                var t = g.First();
                return (t.Id, Status: t.Status.ToString());
            });

        var outstandingActionItems = allMeetings
            .Where(m => (m.MeetingDate ?? m.CreatedAt) >= actionItemCutoff
                        && (m.MeetingDate ?? m.CreatedAt) < dayEnd)
            .SelectMany(m => m.ActionItems
                .Where(ai => !ai.IsCompleted)
                .Select(ai =>
                {
                    var hasLinkedTask = linkedTaskIds.TryGetValue(ai.Id, out var taskInfo);
                    return new OutstandingActionItem(
                        ai.Id, ai.Description, ai.Assignee,
                        m.Id, m.Title, m.MeetingDate,
                        hasLinkedTask, hasLinkedTask ? taskInfo.Id : null,
                        hasLinkedTask ? taskInfo.Status : null);
                }))
            .ToList();

        // Build DTOs
        var meetingItems = meetingsToday.Select(m =>
        {
            var decisions = ParseJsonArrayCount(m.Decisions);
            return new MeetingSummaryItem(
                m.Id, m.Title, m.MeetingDate, m.Attendees, m.Status.ToString(),
                m.Summary, m.ActionItems.Count, decisions,
                m.ActionItems.Count(ai => ai.IsCompleted));
        }).ToList();

        var completedTaskItems = completedTasks.Select(t =>
            new CompletedTaskItem(t.Id, t.Title, t.IsPriority, t.CompletedAt)).ToList();

        var inProgressTaskItems = startedTasks.Select(t =>
            new InProgressTaskItem(t.Id, t.Title, t.IsPriority, t.StartedAt)).ToList();

        var noteItems = notesUpdated.Select(n =>
            new NoteActivityItem(n.Id, NoteTitleExtractor.Extract(n.Content), n.UpdatedAt,
                n.CreatedAt >= dayStart && n.CreatedAt < dayEnd)).ToList();

        var stats = new DailySummaryStats(
            meetingsToday.Count, completedTasks.Count, startedTasks.Count,
            outstandingActionItems.Count, notesUpdated.Count);

        return new DailySummaryDto(date, stats, meetingItems, outstandingActionItems,
            completedTaskItems, inProgressTaskItems, noteItems);
    }

    private static int ParseJsonArrayCount(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return 0;
        try
        {
            var arr = System.Text.Json.JsonSerializer.Deserialize<List<object>>(json);
            return arr?.Count ?? 0;
        }
        catch { return 0; }
    }

}
