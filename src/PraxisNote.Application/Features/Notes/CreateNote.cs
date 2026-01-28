using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Notes.Services;
using PraxisNote.Domain.Aggregates.Notes;

namespace PraxisNote.Application.Features.Notes;

public sealed class CreateNote(
    INoteRepository noteRepository,
    ICheckboxExtractor checkboxExtractor,
    IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, string? Content = null);
    public record Result(Guid NoteId);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var note = Note.Create(command.UserId, command.Content ?? string.Empty);

        // Extract and add checkboxes from content
        if (!string.IsNullOrEmpty(command.Content))
        {
            var checkboxes = checkboxExtractor.Extract(command.Content);
            foreach (var checkbox in checkboxes)
            {
                note.AddCheckbox(checkbox);
            }
        }

        await noteRepository.AddAsync(note, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new Result(note.Id);
    }
}
