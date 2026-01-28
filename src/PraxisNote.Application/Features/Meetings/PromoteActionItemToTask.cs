using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Tasks;
using PraxisNote.Domain.ValueObjects;

using TaskStatus = PraxisNote.Domain.ValueObjects.TaskStatus;

namespace PraxisNote.Application.Features.Meetings;

/// <summary>
/// Promotes an action item within a meeting to a standalone task on the kanban board.
/// </summary>
/// <remarks>
/// This enables users to track action items from meetings on the kanban board.
/// The created task maintains a bidirectional link via ActionItemRef, enabling
/// the UI to show the task status on the action item.
/// </remarks>
public sealed class PromoteActionItemToTask(
    IMeetingRepository meetingRepository,
    ITaskRepository taskRepository,
    IUnitOfWork unitOfWork)
{
    public record Command(Guid MeetingId, Guid UserId, Guid ActionItemId);

    public record Result(
        Guid TaskId,
        string Title,
        string Status);

    public async Task<Result?> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        // 1. Validate meeting exists and belongs to user
        var meeting = await meetingRepository.GetByIdAsync(command.MeetingId, cancellationToken);
        if (meeting is null || meeting.UserId != command.UserId)
            return null;

        // 2. Validate action item exists in meeting
        var actionItem = meeting.GetActionItem(command.ActionItemId);
        if (actionItem is null)
            return null;

        // 3. Check if a task already exists for this action item (idempotency)
        var existingTasks = await taskRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        var existingTask = existingTasks.FirstOrDefault(t =>
            t.ActionItemRef is { } actionItemRef &&
            actionItemRef.MeetingId == command.MeetingId &&
            actionItemRef.ActionItemId == command.ActionItemId);

        if (existingTask is not null)
        {
            // Return existing task instead of creating duplicate
            return new Result(
                existingTask.Id,
                existingTask.Title,
                existingTask.Status.ToString());
        }

        // 4. Push down existing Todo tasks to make room at position 0
        var todoTasks = existingTasks.Where(t => t.Status == TaskStatus.Todo);
        foreach (var todoTask in todoTasks)
        {
            todoTask.SetPosition(todoTask.Position + 1);
        }

        // 5. Create task from action item
        var actionItemRef = new ActionItemRef(command.MeetingId, command.ActionItemId);
        var task = TaskItem.CreateFromActionItem(command.UserId, actionItem.Description, actionItemRef);

        // 6. Set initial status based on action item state
        if (actionItem.IsCompleted)
        {
            task.Complete();
        }

        // 7. Inherit meeting's tags
        foreach (var tagId in meeting.TagIds)
        {
            task.AddTag(tagId);
        }

        // 8. Persist
        await taskRepository.AddAsync(task, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new Result(
            task.Id,
            task.Title,
            task.Status.ToString());
    }
}
