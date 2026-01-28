using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Meetings;

/// <summary>
/// Gets the link status of all action items in a meeting (which are linked to tasks and their status).
/// </summary>
public sealed class GetActionItemStatus(
    IMeetingRepository meetingRepository,
    ITaskRepository taskRepository)
{
    public record Query(Guid MeetingId, Guid UserId);

    public record ActionItemStatusDto(
        Guid ActionItemId,
        bool IsLinked,
        Guid? TaskId,
        string? TaskStatus);

    public async Task<IReadOnlyList<ActionItemStatusDto>?> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var meeting = await meetingRepository.GetByIdAsync(query.MeetingId, cancellationToken);
        if (meeting is null || meeting.UserId != query.UserId)
            return null;

        // Get all tasks linked to this meeting
        // Use GroupBy to handle potential duplicate links gracefully (take first)
        var userTasks = await taskRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        var linkedTasks = userTasks
            .Where(t => t.ActionItemRef?.MeetingId == query.MeetingId)
            .GroupBy(t => t.ActionItemRef!.ActionItemId)
            .ToDictionary(g => g.Key, g => g.First());

        // Build status for each action item
        var result = new List<ActionItemStatusDto>();
        foreach (var actionItem in meeting.ActionItems)
        {
            if (linkedTasks.TryGetValue(actionItem.Id, out var task))
            {
                result.Add(new ActionItemStatusDto(
                    actionItem.Id,
                    IsLinked: true,
                    TaskId: task.Id,
                    TaskStatus: task.Status.ToString()));
            }
            else
            {
                result.Add(new ActionItemStatusDto(
                    actionItem.Id,
                    IsLinked: false,
                    TaskId: null,
                    TaskStatus: null));
            }
        }

        return result;
    }
}
