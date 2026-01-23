using Microsoft.EntityFrameworkCore;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Infrastructure.Persistence.Repositories;

public sealed class TaskRepository(PraxisNoteDbContext context) : ITaskRepository
{
    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Tasks.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Tasks
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> GetTasksWithTagAsync(Guid userId, Guid tagId, CancellationToken cancellationToken = default)
    {
        return await context.Tasks
            .Where(t => t.UserId == userId && t.TagIds.Contains(tagId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetTagUsageCountsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Project only TagIds to avoid loading full entities
        var tagIdLists = await context.Tasks
            .Where(t => t.UserId == userId)
            .Select(t => t.TagIds)
            .ToListAsync(cancellationToken);

        return tagIdLists
            .SelectMany(tagIds => tagIds)
            .GroupBy(tagId => tagId)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await context.Tasks.AddAsync(task, cancellationToken);
    }

    public void Remove(TaskItem task)
    {
        context.Tasks.Remove(task);
    }
}
