using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Tags;

public sealed class GetUserTags(
    ITagRepository tagRepository,
    ITaskRepository taskRepository,
    INoteRepository noteRepository,
    IMeetingRepository meetingRepository)
{
    public record Query(Guid UserId, Guid ProfileId);

    public async Task<IReadOnlyList<TagDto>> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var tags = await tagRepository.GetByUserIdAsync(query.UserId, query.ProfileId, cancellationToken);

        // Sequential — EF Core DbContext is not thread-safe
        var taskCounts = await taskRepository.GetTagUsageCountsAsync(query.UserId, query.ProfileId, cancellationToken);
        var noteCounts = await noteRepository.GetTagUsageCountsAsync(query.UserId, query.ProfileId, cancellationToken);
        var meetingCounts = await meetingRepository.GetTagUsageCountsAsync(query.UserId, query.ProfileId, cancellationToken);

        return tags
            .Select(t =>
            {
                var tc = taskCounts.GetValueOrDefault(t.Id, 0);
                var nc = noteCounts.GetValueOrDefault(t.Id, 0);
                var mc = meetingCounts.GetValueOrDefault(t.Id, 0);
                return new TagDto(t.Id, t.Name, tc + nc + mc, tc, nc, mc);
            })
            .ToList();
    }
}
