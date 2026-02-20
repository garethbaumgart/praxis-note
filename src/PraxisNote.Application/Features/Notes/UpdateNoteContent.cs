using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Notes.Services;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Notes;

public sealed class UpdateNoteContent(
    INoteRepository noteRepository,
    ITaskRepository taskRepository,
    ICheckboxExtractor checkboxExtractor,
    ICheckboxSyncService checkboxSyncService,
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
        var userTasks = await taskRepository.GetByUserIdAsync(command.UserId, note.ProfileId, cancellationToken);
        var linkedTasks = userTasks
            .Where(t => t.CheckboxRef?.NoteId == command.NoteId)
            .ToList();

        // Update the note content
        note.UpdateContent(command.Content);

        // Sync checkboxes and task statuses
        checkboxSyncService.SyncCheckboxes(note, newCheckboxes, linkedTasks);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
