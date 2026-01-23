using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Notes;

namespace PraxisNote.Application.Features.Notes;

public sealed class CreateNote(INoteRepository noteRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, string? Content = null);
    public record Result(Guid NoteId);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var note = Note.Create(command.UserId, command.Content ?? string.Empty);

        await noteRepository.AddAsync(note, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new Result(note.Id);
    }
}
