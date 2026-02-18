using PraxisNote.Application.Features.Notes;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;

namespace PraxisNote.Application.Features.Meetings;

public sealed class GetMeetingNote(
    IMeetingRepository meetingRepository,
    INoteRepository noteRepository)
{
    public record Query(Guid UserId, Guid MeetingId);
    public record Result(string Content, IReadOnlyList<CheckboxDto> Checkboxes);

    public async Task<Result?> ExecuteAsync(Query query, CancellationToken ct = default)
    {
        var meeting = await meetingRepository.GetByIdAsync(query.MeetingId, ct);
        if (meeting is null || meeting.UserId != query.UserId)
        {
            return null;
        }

        if (meeting.NoteId is null)
        {
            return null;
        }

        var note = await noteRepository.GetByIdAsync(meeting.NoteId.Value, ct);
        if (note is null)
        {
            return null;
        }

        return new Result(
            note.Content,
            note.Checkboxes
                .Select(c => new CheckboxDto(c.Id, c.Text, c.IsChecked))
                .ToList());
    }
}
