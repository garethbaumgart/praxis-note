using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Features.Meetings;

public sealed class ClearTranscript(IMeetingRepository meetingRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid MeetingId, Guid UserId);

    public async Task<bool> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var meeting = await meetingRepository.GetByIdAsync(command.MeetingId, cancellationToken);

        if (meeting is null || meeting.UserId != command.UserId)
            return false;

        meeting.ClearTranscript();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
