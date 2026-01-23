using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tags;

namespace PraxisNote.Application.Features.Notes;

public sealed class GetNoteById(INoteRepository noteRepository, ITagRepository tagRepository)
{
    public record Query(Guid NoteId, Guid UserId);

    public async Task<NoteDto?> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var note = await noteRepository.GetByIdAsync(query.NoteId, cancellationToken);

        if (note is null || note.UserId != query.UserId)
            return null;

        var tags = await tagRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        var tagLookup = tags.ToDictionary(t => t.Id);

        return new NoteDto(
            note.Id,
            note.Content,
            note.Checkboxes
                .Select(c => new CheckboxDto(c.Id, c.Text, c.IsChecked))
                .ToList(),
            note.TagIds
                .Where(id => tagLookup.ContainsKey(id))
                .Select(id => new NoteTagDto(id, tagLookup[id].Name))
                .ToList(),
            note.CreatedAt,
            note.UpdatedAt);
    }
}
