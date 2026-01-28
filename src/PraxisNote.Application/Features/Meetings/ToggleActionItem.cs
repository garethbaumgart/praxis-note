using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Features.Meetings;

public sealed class ToggleActionItem(IMeetingRepository meetingRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid MeetingId, Guid ActionItemId);

    public const string MeetingNotFoundError = "MEETING_NOT_FOUND";
    public const string ActionItemNotFoundError = "ACTION_ITEM_NOT_FOUND";

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var meeting = await meetingRepository.GetByIdAsync(command.MeetingId, cancellationToken);
        if (meeting is null || meeting.UserId != command.UserId)
        {
            throw new InvalidOperationException(MeetingNotFoundError);
        }

        var toggled = meeting.ToggleActionItem(command.ActionItemId);
        if (!toggled)
        {
            throw new InvalidOperationException(ActionItemNotFoundError);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
