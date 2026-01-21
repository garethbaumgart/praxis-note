using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Tags;

public sealed class GetUserTags(ITagRepository tagRepository, ITaskRepository taskRepository)
{
    public record Query(Guid UserId);

    public async Task<IReadOnlyList<TagDto>> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var tags = await tagRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        var tasks = await taskRepository.GetByUserIdAsync(query.UserId, cancellationToken);

        // Count tag usage across all tasks
        var usageCounts = tasks
            .SelectMany(t => t.TagIds)
            .GroupBy(tagId => tagId)
            .ToDictionary(g => g.Key, g => g.Count());

        return tags
            .Select(t => new TagDto(
                t.Id,
                t.Name,
                t.Color,
                usageCounts.GetValueOrDefault(t.Id, 0)))
            .ToList();
    }
}
