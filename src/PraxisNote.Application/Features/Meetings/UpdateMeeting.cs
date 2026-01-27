using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Features.Meetings;

public sealed class UpdateMeeting(IMeetingRepository meetingRepository, IUnitOfWork unitOfWork)
{
    public record Command(
        Guid MeetingId,
        Guid UserId,
        string? Title = null,
        DateTimeOffset? MeetingDate = null,
        string? Attendees = null);

    public async Task<bool> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var meeting = await meetingRepository.GetByIdAsync(command.MeetingId, cancellationToken);

        if (meeting is null || meeting.UserId != command.UserId)
            return false;

        meeting.UpdateTitle(command.Title);
        meeting.UpdateMeetingDate(command.MeetingDate);
        meeting.UpdateAttendees(command.Attendees);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
