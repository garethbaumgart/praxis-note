using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Tags;

namespace PraxisNote.Application.Features.Meetings;

public sealed class AddTagToMeeting(IMeetingRepository meetingRepository, ITagRepository tagRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid MeetingId, Guid TagId);

    public const string MeetingNotFoundError = "MEETING_NOT_FOUND";
    public const string TagNotFoundError = "TAG_NOT_FOUND";

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var meeting = await meetingRepository.GetByIdAsync(command.MeetingId, cancellationToken);
        if (meeting is null || meeting.UserId != command.UserId)
        {
            throw new InvalidOperationException(MeetingNotFoundError);
        }

        var tag = await tagRepository.GetByIdAsync(command.TagId, cancellationToken);
        if (tag is null || tag.UserId != command.UserId)
        {
            throw new InvalidOperationException(TagNotFoundError);
        }

        meeting.AddTag(command.TagId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
