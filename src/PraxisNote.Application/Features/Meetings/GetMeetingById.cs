using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Tags;

namespace PraxisNote.Application.Features.Meetings;

public sealed class GetMeetingById(IMeetingRepository meetingRepository, ITagRepository tagRepository)
{
    public record Query(Guid MeetingId, Guid UserId);

    public async Task<MeetingDto?> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var meeting = await meetingRepository.GetByIdAsync(query.MeetingId, cancellationToken);

        if (meeting is null || meeting.UserId != query.UserId)
            return null;

        var tags = await tagRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        var tagLookup = tags.ToDictionary(t => t.Id);

        return MapToDto(meeting, tagLookup);
    }

    internal static MeetingDto MapToDto(Meeting meeting, Dictionary<Guid, Tag> tagLookup)
    {
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
            meeting.BehavioralAnalysis,
            meeting.TagIds
                .Where(id => tagLookup.ContainsKey(id))
                .Select(id => new MeetingTagDto(id, tagLookup[id].Name))
                .ToList(),
            meeting.ActionItems
                .Select(a => new ActionItemDto(a.Id, a.Description, a.Assignee, a.IsCompleted, a.CreatedAt))
                .ToList(),
            meeting.CreatedAt,
            meeting.UpdatedAt);
    }
}
