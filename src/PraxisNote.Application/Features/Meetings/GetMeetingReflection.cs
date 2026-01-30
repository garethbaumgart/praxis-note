using System.Text.Json;
using PraxisNote.Domain.Aggregates.Meetings;

namespace PraxisNote.Application.Features.Meetings;

public sealed class GetMeetingReflection(IMeetingRepository meetingRepository)
{
    public record Query(Guid MeetingId, Guid UserId);

    public async Task<ReflectionDto?> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var meeting = await meetingRepository.GetByIdAsync(query.MeetingId, cancellationToken);

        if (meeting is null || meeting.UserId != query.UserId)
            return null;

        if (meeting.ReflectionData is null)
            return null;

        try
        {
            return JsonSerializer.Deserialize<ReflectionDto>(meeting.ReflectionData, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
