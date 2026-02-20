using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tasks;
using PraxisNote.Domain.ValueObjects;

using TaskStatus = PraxisNote.Domain.ValueObjects.TaskStatus;

namespace PraxisNote.Application.Features.Notes.Services;

/// <summary>
/// Service for syncing checkboxes between note content and the note domain entity.
/// </summary>
public interface ICheckboxSyncService
{
    /// <summary>
    /// Syncs checkboxes from content to the note entity and task statuses.
    /// </summary>
    void SyncCheckboxes(
        Note note,
        IReadOnlyList<Checkbox> newCheckboxes,
        IEnumerable<TaskItem> linkedTasks);
}

/// <summary>
/// Implementation of <see cref="ICheckboxSyncService"/>.
/// </summary>
public sealed class CheckboxSyncService : ICheckboxSyncService
{
    public void SyncCheckboxes(
        Note note,
        IReadOnlyList<Checkbox> newCheckboxes,
        IEnumerable<TaskItem> linkedTasks)
    {
        // Sync task statuses based on checkbox changes
        foreach (var task in linkedTasks)
        {
            var checkbox = newCheckboxes.FirstOrDefault(c => c.Id == task.CheckboxRef!.CheckboxId);
            if (checkbox is null)
                continue; // Checkbox was removed - task becomes unlinked but persists

            SyncTaskStatusFromCheckbox(task, checkbox);
        }

        // Sync the note's checkbox collection with extracted checkboxes
        SyncNoteCheckboxes(note, newCheckboxes);
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
