using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Features.Meetings;

public sealed class GetUserMeetings(IMeetingRepository meetingRepository)
{
    public record Query(Guid UserId);

    public async Task<IReadOnlyList<MeetingDto>> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var meetings = await meetingRepository.GetByUserIdAsync(query.UserId, cancellationToken);

        return meetings
            .Select(m => new MeetingDto(
                m.Id,
                m.Title,
                m.MeetingDate,
                m.Attendees,
                m.Status.ToString(),
                m.CreatedAt,
                m.UpdatedAt))
            .ToList();
    }
}
