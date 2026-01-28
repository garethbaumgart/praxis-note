using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Notes;

namespace PraxisNote.Application.Features.Notes;

public sealed class RemoveTagFromNote(INoteRepository noteRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid NoteId, Guid TagId);

    public const string NoteNotFoundError = "NOTE_NOT_FOUND";

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var note = await noteRepository.GetByIdAsync(command.NoteId, cancellationToken);
        if (note is null || note.UserId != command.UserId)
        {
            throw new InvalidOperationException(NoteNotFoundError);
        }

        note.RemoveTag(command.TagId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
