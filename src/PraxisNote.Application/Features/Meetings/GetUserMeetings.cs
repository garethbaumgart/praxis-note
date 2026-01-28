using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Tags;

namespace PraxisNote.Application.Features.Meetings;

public sealed class GetUserMeetings(IMeetingRepository meetingRepository, ITagRepository tagRepository)
{
    public record Query(Guid UserId);

    public async Task<IReadOnlyList<MeetingDto>> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var meetings = await meetingRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        var tags = await tagRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        var tagLookup = tags.ToDictionary(t => t.Id);

        return meetings
            .Select(m => GetMeetingById.MapToDto(m, tagLookup))
            .ToList();
    }
}
