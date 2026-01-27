using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Features.Meetings;

public sealed class SubmitTranscript(IMeetingRepository meetingRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid MeetingId, Guid UserId, string Transcript);

    public async Task<bool> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var meeting = await meetingRepository.GetByIdAsync(command.MeetingId, cancellationToken);

        if (meeting is null || meeting.UserId != command.UserId)
            return false;

        meeting.SubmitTranscript(command.Transcript);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
