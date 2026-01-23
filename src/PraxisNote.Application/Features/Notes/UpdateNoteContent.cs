using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Notes.Services;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tasks;
using PraxisNote.Domain.ValueObjects;

using TaskStatus = PraxisNote.Domain.ValueObjects.TaskStatus;

namespace PraxisNote.Application.Features.Notes;

public sealed class UpdateNoteContent(
    INoteRepository noteRepository,
    ITaskRepository taskRepository,
    ICheckboxExtractor checkboxExtractor,
    IUnitOfWork unitOfWork)
{
    public record Command(Guid NoteId, Guid UserId, string Content);

    public async Task<bool> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var note = await noteRepository.GetByIdAsync(command.NoteId, cancellationToken);

        if (note is null || note.UserId != command.UserId)
            return false;

        // Extract checkboxes from the new content
        var newCheckboxes = checkboxExtractor.Extract(command.Content);

        // Get all tasks for this user that are linked to this note
        var userTasks = await taskRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        var linkedTasks = userTasks
            .Where(t => t.CheckboxRef?.NoteId == command.NoteId)
            .ToList();

        // Sync task statuses based on checkbox changes
        foreach (var task in linkedTasks)
        {
            var checkbox = newCheckboxes.FirstOrDefault(c => c.Id == task.CheckboxRef!.CheckboxId);
            if (checkbox is null)
                continue; // Checkbox was removed - task becomes unlinked but persists

            SyncTaskStatusFromCheckbox(task, checkbox);
        }

        // Update the note content
        note.UpdateContent(command.Content);

        // Sync the note's checkbox collection with extracted checkboxes
        SyncNoteCheckboxes(note, newCheckboxes);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Syncs task status based on checkbox state:
    /// - Checkbox checked → Task Done
    /// - Checkbox unchecked → Task Todo (if it was Done)
    /// </summary>
    private static void SyncTaskStatusFromCheckbox(TaskItem task, Checkbox checkbox)
    {
        if (checkbox.IsChecked && task.Status != TaskStatus.Done)
        {
            task.Complete();
        }
        else if (!checkbox.IsChecked && task.Status == TaskStatus.Done)
        {
            task.Reopen();
        }
        // InProgress tasks are not affected by checkbox changes
    }

    /// <summary>
    /// Syncs the note's checkbox collection with the extracted checkboxes.
    /// </summary>
    private static void SyncNoteCheckboxes(Note note, IReadOnlyList<Checkbox> newCheckboxes)
    {
        // Get IDs of checkboxes that should exist
        var newCheckboxIds = newCheckboxes.Select(c => c.Id).ToHashSet();

        // Remove checkboxes that no longer exist
        var existingCheckboxIds = note.Checkboxes.Select(c => c.Id).ToList();
        foreach (var checkboxId in existingCheckboxIds)
        {
            if (!newCheckboxIds.Contains(checkboxId))
            {
                note.RemoveCheckbox(checkboxId);
            }
        }

        // Add or update checkboxes
        foreach (var checkbox in newCheckboxes)
        {
            if (note.HasCheckbox(checkbox.Id))
            {
                note.UpdateCheckbox(checkbox.Id, checkbox.Text, checkbox.IsChecked);
            }
            else
            {
                note.AddCheckbox(checkbox);
            }
        }
    }
}
