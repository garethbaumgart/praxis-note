using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Notes;

namespace PraxisNote.Application.Features.Notes;

public sealed class AddTagToNote(INoteRepository noteRepository, ITagRepository tagRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid NoteId, Guid TagId);

    public const string NoteNotFoundError = "NOTE_NOT_FOUND";
    public const string TagNotFoundError = "TAG_NOT_FOUND";

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var note = await noteRepository.GetByIdAsync(command.NoteId, cancellationToken);
        if (note is null || note.UserId != command.UserId)
        {
            throw new InvalidOperationException(NoteNotFoundError);
        }

        var tag = await tagRepository.GetByIdAsync(command.TagId, cancellationToken);
        if (tag is null || tag.UserId != command.UserId)
        {
            throw new InvalidOperationException(TagNotFoundError);
        }

        note.AddTag(command.TagId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
