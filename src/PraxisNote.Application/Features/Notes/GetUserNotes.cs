using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tags;

namespace PraxisNote.Application.Features.Notes;

public sealed class GetUserNotes(INoteRepository noteRepository, ITagRepository tagRepository, IMeetingRepository meetingRepository)
{
    public record Query(Guid UserId, Guid ProfileId);

    public async Task<IReadOnlyList<NoteDto>> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var notes = await noteRepository.GetByUserIdAsync(query.UserId, query.ProfileId, cancellationToken);
        var tags = await tagRepository.GetByUserIdAsync(query.UserId, query.ProfileId, cancellationToken);
        var tagLookup = tags.ToDictionary(t => t.Id);

        // Build NoteId -> (MeetingId, Title) lookup for meeting-linked notes
        var meetings = await meetingRepository.GetByUserIdAsync(query.UserId, query.ProfileId, cancellationToken);
        var meetingByNoteId = meetings
            .Where(m => m.NoteId is not null)
            .ToDictionary(m => m.NoteId!.Value, m => (MeetingId: m.Id, m.Title));

        return notes
            .Select(n =>
            {
                meetingByNoteId.TryGetValue(n.Id, out var meetingRef);
                return new NoteDto(
                    n.Id,
                    n.Content,
                    n.Checkboxes
                        .Select(c => new CheckboxDto(c.Id, c.Text, c.IsChecked))
                        .ToList(),
                    n.TagIds
                        .Where(id => tagLookup.ContainsKey(id))
                        .Select(id => new NoteTagDto(id, tagLookup[id].Name))
                        .ToList(),
                    meetingRef.MeetingId == default ? null : meetingRef.MeetingId,
                    meetingRef.Title,
                    n.CreatedAt,
                    n.UpdatedAt);
            })
            .ToList();
    }
}
