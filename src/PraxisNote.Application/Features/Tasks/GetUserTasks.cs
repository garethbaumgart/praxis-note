using Microsoft.Extensions.Options;
using PraxisNote.Domain.Aggregates.Tasks;
using TaskStatus = PraxisNote.Domain.ValueObjects.TaskStatus;

namespace PraxisNote.Application.Features.Tasks;

public sealed class GetUserTasks(ITaskRepository taskRepository, IOptions<TaskSettings> settings)
{
    private readonly TaskSettings _settings = settings.Value;

    public record Query(Guid UserId, bool IncludeArchived = false);

    public async Task<IReadOnlyList<TaskDto>> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var tasks = await taskRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        var archiveThreshold = DateTimeOffset.UtcNow.AddDays(-_settings.ArchiveThresholdDays);

        var filteredTasks = query.IncludeArchived
            ? tasks
                .Where(t => t.Status == TaskStatus.Done
                    && t.CompletedAt.HasValue
                    && t.CompletedAt.Value < archiveThreshold)
                .OrderByDescending(t => t.CompletedAt)
                .Take(_settings.MaxArchivedTasks)
            : tasks
                .Where(t => t.Status != TaskStatus.Done
                    || !t.CompletedAt.HasValue
                    || t.CompletedAt.Value >= archiveThreshold)
                .OrderBy(t => t.Position);

        return filteredTasks
            .Select(t => new TaskDto(
                t.Id,
                t.Title,
                t.Status.ToString(),
                t.Position,
                t.CreatedAt,
                t.StartedAt,
                t.CompletedAt,
                t.Comments
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new CommentDto(c.Id, c.Content, c.CreatedAt, c.UpdatedAt))
                    .ToList(),
                t.DueDate?.Date))
            .ToList();
    }
}
