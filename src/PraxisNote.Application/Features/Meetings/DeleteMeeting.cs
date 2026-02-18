using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;

namespace PraxisNote.Application.Features.Meetings;

public sealed class DeleteMeeting(IMeetingRepository meetingRepository, INoteRepository noteRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid MeetingId, Guid UserId);

    public async Task<bool> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var meeting = await meetingRepository.GetByIdAsync(command.MeetingId, cancellationToken);

        if (meeting is null || meeting.UserId != command.UserId)
            return false;

        // Cascade-delete linked note
        if (meeting.NoteId is not null)
        {
            var note = await noteRepository.GetByIdAsync(meeting.NoteId.Value, cancellationToken);
            if (note is not null)
            {
                noteRepository.Remove(note);
            }
        }

        meetingRepository.Remove(meeting);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
