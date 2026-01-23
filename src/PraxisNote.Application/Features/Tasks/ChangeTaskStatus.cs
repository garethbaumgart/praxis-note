using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Notes.Services;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tasks;

using TaskStatus = PraxisNote.Domain.ValueObjects.TaskStatus;

namespace PraxisNote.Application.Features.Tasks;

public sealed class ChangeTaskStatus(
    ITaskRepository taskRepository,
    INoteRepository noteRepository,
    ICheckboxUpdater checkboxUpdater,
    IUnitOfWork unitOfWork)
{
    public record Command(Guid TaskId, Guid UserId, string TargetStatus, int? Position = null);

    public async Task<bool> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var task = await taskRepository.GetByIdAsync(command.TaskId, cancellationToken);

        if (task is null || task.UserId != command.UserId)
        {
            return false;
        }

        if (!Enum.TryParse<TaskStatus>(command.TargetStatus, ignoreCase: true, out var targetStatus))
        {
            return false;
        }

        var allTasks = await taskRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        var tasksInTargetColumn = allTasks
            .Where(t => t.Status == targetStatus && t.Id != task.Id)
            .OrderBy(t => t.Position)
            .ToList();

        // Calculate target position (clamp to valid range)
        var targetPosition = command.Position ?? 0;
        targetPosition = Math.Max(0, Math.Min(targetPosition, tasksInTargetColumn.Count));

        // Rebuild positions: insert moved task at targetPosition, shift others
        for (var i = 0; i < tasksInTargetColumn.Count; i++)
        {
            var newPosition = i >= targetPosition ? i + 1 : i;
            tasksInTargetColumn[i].SetPosition(newPosition);
        }
        task.SetPosition(targetPosition);

        switch (targetStatus)
        {
            case TaskStatus.Todo:
                task.Reopen();
                break;
            case TaskStatus.InProgress:
                task.Start();
                break;
            case TaskStatus.Done:
                task.Complete();
                break;
        }

        // Sync checkbox state if task is linked to a note
        await SyncCheckboxState(task, targetStatus, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Updates the linked checkbox state when task status changes.
    /// - Done → checkbox checked
    /// - Todo/InProgress → checkbox unchecked
    /// </summary>
    private async Task SyncCheckboxState(TaskItem task, TaskStatus targetStatus, CancellationToken cancellationToken)
    {
        if (!task.IsLinkedToNote || task.CheckboxRef is null)
            return;

        var note = await noteRepository.GetByIdAsync(task.CheckboxRef.NoteId, cancellationToken);
        if (note is null)
            return;

        var shouldBeChecked = targetStatus == TaskStatus.Done;
        var updatedContent = checkboxUpdater.UpdateCheckboxState(
            note.Content,
            task.CheckboxRef.CheckboxId,
            shouldBeChecked);

        if (updatedContent is not null)
        {
            note.UpdateContent(updatedContent);
        }
    }
}
