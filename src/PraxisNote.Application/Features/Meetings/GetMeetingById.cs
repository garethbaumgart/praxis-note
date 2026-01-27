using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Features.Meetings;

public sealed class GetMeetingById(IMeetingRepository meetingRepository)
{
    public record Query(Guid MeetingId, Guid UserId);

    public async Task<MeetingDto?> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var meeting = await meetingRepository.GetByIdAsync(query.MeetingId, cancellationToken);

        if (meeting is null || meeting.UserId != query.UserId)
            return null;

        return new MeetingDto(
            meeting.Id,
            meeting.Title,
            meeting.MeetingDate,
            meeting.Attendees,
            meeting.TranscriptContent,
            meeting.Status.ToString(),
            meeting.Summary,
            meeting.KeyPoints,
            meeting.Decisions,
            meeting.CreatedAt,
            meeting.UpdatedAt);
    }
}
