using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Tasks;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tasks;
using PraxisNote.Domain.ValueObjects;

using TaskStatus = PraxisNote.Domain.ValueObjects.TaskStatus;

namespace PraxisNote.Application.Features.Notes;

/// <summary>
/// Promotes a checkbox within a note to a standalone task on the kanban board.
/// </summary>
/// <remarks>
/// This is the core feature of PraxisNote: turning note checkboxes into trackable tasks.
/// The created task maintains a bidirectional link via CheckboxRef, enabling:
/// - Checkbox state changes to update task status
/// - Task status changes to update checkbox state
/// </remarks>
public sealed class PromoteCheckboxToTask(
    INoteRepository noteRepository,
    ITaskRepository taskRepository,
    IUnitOfWork unitOfWork)
{
    public record Command(Guid NoteId, Guid UserId, string CheckboxId);

    public record Result(
        Guid TaskId,
        string Title,
        string Status);

    public async Task<Result?> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        // 1. Validate note exists and belongs to user
        var note = await noteRepository.GetByIdAsync(command.NoteId, cancellationToken);
        if (note is null || note.UserId != command.UserId)
            return null;

        // 2. Validate checkbox exists in note
        var checkbox = note.GetCheckbox(command.CheckboxId);
        if (checkbox is null)
            return null;

        // 3. Check if a task already exists for this checkbox
        var existingTasks = await taskRepository.GetByUserIdAsync(command.UserId, note.ProfileId, cancellationToken);
        var existingTask = existingTasks.FirstOrDefault(t =>
            t.CheckboxRef is { } checkboxRef &&
            checkboxRef.NoteId == command.NoteId &&
            checkboxRef.CheckboxId == command.CheckboxId);

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

        // 5. Create task from checkbox
        var checkboxRef = new CheckboxRef(command.NoteId, command.CheckboxId);
        var task = TaskItem.CreateFromCheckbox(command.UserId, note.ProfileId, checkbox.Text, checkboxRef);

        // 6. Set initial status based on checkbox state
        if (checkbox.IsChecked)
        {
            task.Complete();
        }

        // 7. Inherit note's tags
        foreach (var tagId in note.TagIds)
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
