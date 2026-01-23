using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Tags;

public sealed class GetUserTags(ITagRepository tagRepository, ITaskRepository taskRepository)
{
    public record Query(Guid UserId);

    public async Task<IReadOnlyList<TagDto>> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var tags = await tagRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        var usageCounts = await taskRepository.GetTagUsageCountsAsync(query.UserId, cancellationToken);

        return tags
            .Select(t => new TagDto(
                t.Id,
                t.Name,
                usageCounts.GetValueOrDefault(t.Id, 0)))
            .ToList();
    }
}
