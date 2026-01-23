namespace PraxisNote.Domain.Aggregates.Tasks;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> GetTasksWithTagAsync(Guid userId, Guid tagId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, int>> GetTagUsageCountsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);
    void Remove(TaskItem task);
}
