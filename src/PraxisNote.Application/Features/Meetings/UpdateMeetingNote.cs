using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Notes.Services;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tasks;
using PraxisNote.Domain.ValueObjects;

using TaskStatus = PraxisNote.Domain.ValueObjects.TaskStatus;

namespace PraxisNote.Application.Features.Meetings;

public sealed class UpdateMeetingNote(
    IMeetingRepository meetingRepository,
    INoteRepository noteRepository,
    ICheckboxExtractor checkboxExtractor,
    ITaskRepository taskRepository,
    IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid MeetingId, string Content);

    public const string MeetingNotFoundError = "MEETING_NOT_FOUND";
    public const string NoNoteLinkedError = "NO_NOTE_LINKED";

    public async Task ExecuteAsync(Command command, CancellationToken ct = default)
    {
        var meeting = await meetingRepository.GetByIdAsync(command.MeetingId, ct);
        if (meeting is null || meeting.UserId != command.UserId)
        {
            throw new InvalidOperationException(MeetingNotFoundError);
        }

        if (meeting.NoteId is null)
        {
            throw new InvalidOperationException(NoNoteLinkedError);
        }

        var note = await noteRepository.GetByIdAsync(meeting.NoteId.Value, ct);
        if (note is null)
        {
            throw new InvalidOperationException(NoNoteLinkedError);
        }

        // Extract checkboxes from the new content
        var newCheckboxes = checkboxExtractor.Extract(command.Content);

        // Get all tasks for this user that are linked to this note
        var userTasks = await taskRepository.GetByUserIdAsync(command.UserId, note.ProfileId, ct);
        var linkedTasks = userTasks
            .Where(t => t.CheckboxRef?.NoteId == meeting.NoteId.Value)
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

        await unitOfWork.SaveChangesAsync(ct);
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
