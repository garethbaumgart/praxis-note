using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Tasks;

public sealed class GetUserTasks(ITaskRepository taskRepository)
{
    public record Query(Guid UserId);

    public async Task<IReadOnlyList<TaskDto>> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var tasks = await taskRepository.GetByUserIdAsync(query.UserId, cancellationToken);

        return tasks
            .OrderBy(t => t.Position)
            .Select(t => new TaskDto(
                t.Id,
                t.Title,
                t.Status.ToString(),
                t.Position,
                t.CreatedAt,
                t.CompletedAt))
            .ToList();
    }
}
