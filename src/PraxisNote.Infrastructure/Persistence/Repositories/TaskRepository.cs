using Microsoft.EntityFrameworkCore;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Infrastructure.Persistence.Repositories;

public sealed class TaskRepository(PraxisNoteDbContext context) : ITaskRepository
{
    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Tasks.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> GetByUserIdAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default)
    {
        return await context.Tasks
            .Where(t => t.UserId == userId && t.ProfileId == profileId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> GetTasksWithTagAsync(Guid userId, Guid profileId, Guid tagId, CancellationToken cancellationToken = default)
    {
        // In-memory filtering required because TagIds uses a JSON value conversion
        // that EF Core can't translate Contains() on. Same pattern as GetTagUsageCountsAsync.
        var tasks = await context.Tasks
            .Where(t => t.UserId == userId && t.ProfileId == profileId)
            .ToListAsync(cancellationToken);

        return tasks.Where(t => t.TagIds.Contains(tagId)).ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetTagUsageCountsAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default)
    {
        // Project only TagIds to minimize data transfer (TagIds is stored as JSON).
        // In-memory aggregation is necessary because EF Core can't translate
        // SelectMany/GroupBy on JSON arrays to SQL without raw queries.
        var tagIdLists = await context.Tasks
            .Where(t => t.UserId == userId && t.ProfileId == profileId)
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
