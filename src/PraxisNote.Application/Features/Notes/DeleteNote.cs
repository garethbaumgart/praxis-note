using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Notes;

namespace PraxisNote.Application.Features.Notes;

public sealed class DeleteNote(INoteRepository noteRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid NoteId, Guid UserId);

    public async Task<bool> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var note = await noteRepository.GetByIdAsync(command.NoteId, cancellationToken);

        if (note is null || note.UserId != command.UserId)
            return false;

        noteRepository.Remove(note);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
