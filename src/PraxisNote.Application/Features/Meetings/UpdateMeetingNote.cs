using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Notes.Services;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Meetings;

public sealed class UpdateMeetingNote(
    IMeetingRepository meetingRepository,
    INoteRepository noteRepository,
    ITaskRepository taskRepository,
    ICheckboxExtractor checkboxExtractor,
    ICheckboxSyncService checkboxSyncService,
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

        // Update the note content
        note.UpdateContent(command.Content);

        // Sync checkboxes and task statuses
        checkboxSyncService.SyncCheckboxes(note, newCheckboxes, linkedTasks);

        await unitOfWork.SaveChangesAsync(ct);
    }
}
