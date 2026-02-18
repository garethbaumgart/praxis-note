using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;

namespace PraxisNote.Application.Features.Meetings;

public sealed class RemoveTagFromMeeting(IMeetingRepository meetingRepository, INoteRepository noteRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid MeetingId, Guid TagId);

    public const string MeetingNotFoundError = "MEETING_NOT_FOUND";

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var meeting = await meetingRepository.GetByIdAsync(command.MeetingId, cancellationToken);
        if (meeting is null || meeting.UserId != command.UserId)
        {
            throw new InvalidOperationException(MeetingNotFoundError);
        }

        meeting.RemoveTag(command.TagId);

        // Sync tag removal to linked note
        if (meeting.NoteId is not null)
        {
            var note = await noteRepository.GetByIdAsync(meeting.NoteId.Value, cancellationToken);
            note?.RemoveTag(command.TagId);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
