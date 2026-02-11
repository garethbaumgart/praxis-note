using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tags;

namespace PraxisNote.Application.Features.Notes;

public sealed class GetUserNotes(INoteRepository noteRepository, ITagRepository tagRepository)
{
    public record Query(Guid UserId, Guid ProfileId);

    public async Task<IReadOnlyList<NoteDto>> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var notes = await noteRepository.GetByUserIdAsync(query.UserId, query.ProfileId, cancellationToken);
        var tags = await tagRepository.GetByUserIdAsync(query.UserId, query.ProfileId, cancellationToken);
        var tagLookup = tags.ToDictionary(t => t.Id);

        return notes
            .Select(n => new NoteDto(
                n.Id,
                n.Content,
                n.Checkboxes
                    .Select(c => new CheckboxDto(c.Id, c.Text, c.IsChecked))
                    .ToList(),
                n.TagIds
                    .Where(id => tagLookup.ContainsKey(id))
                    .Select(id => new NoteTagDto(id, tagLookup[id].Name))
                    .ToList(),
                n.CreatedAt,
                n.UpdatedAt))
            .ToList();
    }
}
