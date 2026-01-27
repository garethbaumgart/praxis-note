using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Features.Meetings;

public sealed class CreateMeeting(IMeetingRepository meetingRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, string? Title = null, DateTimeOffset? MeetingDate = null, string? Attendees = null);
    public record Result(Guid MeetingId);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var meeting = Meeting.Create(command.UserId, command.Title, command.MeetingDate);

        if (!string.IsNullOrWhiteSpace(command.Attendees))
        {
            meeting.UpdateAttendees(command.Attendees);
        }

        await meetingRepository.AddAsync(meeting, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new Result(meeting.Id);
    }
}
