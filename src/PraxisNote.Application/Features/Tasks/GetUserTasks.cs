using PraxisNote.Domain.Aggregates.Tasks;
using TaskStatus = PraxisNote.Domain.ValueObjects.TaskStatus;

namespace PraxisNote.Application.Features.Tasks;

public sealed class GetUserTasks(ITaskRepository taskRepository)
{
    private const int ArchiveThresholdDays = 7;
    private const int MaxArchivedTasks = 50;

    public record Query(Guid UserId, bool IncludeArchived = false);

    public async Task<IReadOnlyList<TaskDto>> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var tasks = await taskRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        var archiveThreshold = DateTimeOffset.UtcNow.AddDays(-ArchiveThresholdDays);

        IEnumerable<TaskItem> filteredTasks;

        if (query.IncludeArchived)
        {
            // Return only archived Done tasks (completed more than 7 days ago)
            filteredTasks = tasks
                .Where(t => t.Status == TaskStatus.Done
                    && t.CompletedAt.HasValue
                    && t.CompletedAt.Value < archiveThreshold)
                .OrderByDescending(t => t.CompletedAt)
                .Take(MaxArchivedTasks);
        }
        else
        {
            // Exclude archived Done tasks from normal view
            filteredTasks = tasks
                .Where(t => t.Status != TaskStatus.Done
                    || !t.CompletedAt.HasValue
                    || t.CompletedAt.Value >= archiveThreshold)
                .OrderBy(t => t.Position);
        }

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
