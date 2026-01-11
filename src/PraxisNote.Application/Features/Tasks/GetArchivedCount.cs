using Microsoft.Extensions.Options;
using PraxisNote.Domain.Aggregates.Tasks;
using TaskStatus = PraxisNote.Domain.ValueObjects.TaskStatus;

namespace PraxisNote.Application.Features.Tasks;

public sealed class GetArchivedCount(ITaskRepository taskRepository, IOptions<TaskSettings> settings)
{
    private readonly TaskSettings _settings = settings.Value;

    public record Query(Guid UserId);

    public async Task<int> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var tasks = await taskRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        var archiveThreshold = DateTimeOffset.UtcNow.AddDays(-_settings.ArchiveThresholdDays);

        return tasks.Count(t =>
            t.Status == TaskStatus.Done
            && t.CompletedAt.HasValue
            && t.CompletedAt.Value < archiveThreshold);
    }
}
