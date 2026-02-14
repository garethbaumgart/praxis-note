using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.ActionItems;

public sealed class GetOutstandingActionItems(
    IMeetingRepository meetingRepository,
    ITaskRepository taskRepository)
{
    public record Query(Guid UserId, Guid ProfileId);

    public async Task<List<OutstandingActionItemDto>> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var cutoff = now.AddDays(-30);

        // Load data sequentially — EF Core DbContext is not thread-safe
        var allMeetings = await meetingRepository.GetByUserIdAsync(query.UserId, query.ProfileId, cancellationToken);
        var allTasks = await taskRepository.GetByUserIdAsync(query.UserId, query.ProfileId, cancellationToken);

        // Build lookup of linked task statuses by action item ID
        var linkedTaskIds = allTasks
            .Where(t => t.ActionItemRef is not null && t.ActionItemRef.IsLinked)
            .GroupBy(t => t.ActionItemRef!.ActionItemId)
            .ToDictionary(g => g.Key, g =>
            {
                var t = g.First();
                return (t.Id, Status: t.Status.ToString());
            });

        // Outstanding action items: uncompleted from meetings in last 30 days
        var outstandingActionItems = allMeetings
            .Where(m => (m.MeetingDate ?? m.CreatedAt) >= cutoff
                        && (m.MeetingDate ?? m.CreatedAt) <= now)
            .SelectMany(m => m.ActionItems
                .Where(ai => !ai.IsCompleted)
                .Select(ai =>
                {
                    var hasLinkedTask = linkedTaskIds.TryGetValue(ai.Id, out var taskInfo);
                    return new OutstandingActionItemDto(
                        ai.Id, ai.Description, ai.Assignee,
                        m.Id, m.Title, m.MeetingDate,
                        hasLinkedTask, hasLinkedTask ? taskInfo.Id : null,
                        hasLinkedTask ? taskInfo.Status : null);
                }))
            .ToList();

        return outstandingActionItems;
    }
}
