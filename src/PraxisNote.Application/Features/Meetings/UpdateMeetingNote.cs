using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;

namespace PraxisNote.Application.Features.Meetings;

public sealed class UpdateMeetingNote(
    IMeetingRepository meetingRepository,
    INoteRepository noteRepository,
    IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid MeetingId, string Content);

    public const string MeetingNotFoundError = "MEETING_NOT_FOUND";
    public const string NoNoteLinkedError = "NO_NOTE_LINKED";

    public async Task ExecuteAsync(Command command, CancellationToken ct = default)
    {
        var meeting = await meetingRepository.GetByIdAsync(command.MeetingId, ct);
        if (meeting is null || meeting.UserId != command.UserId)
        {
            throw new InvalidOperationException(MeetingNotFoundError);
        }

        if (meeting.NoteId is null)
        {
            throw new InvalidOperationException(NoNoteLinkedError);
        }

        var note = await noteRepository.GetByIdAsync(meeting.NoteId.Value, ct);
        if (note is null)
        {
            throw new InvalidOperationException(NoNoteLinkedError);
        }

        note.UpdateContent(command.Content);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
