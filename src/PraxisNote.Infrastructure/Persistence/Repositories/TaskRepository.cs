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
        var tasks = await context.Tasks
            .Where(t => t.UserId == userId)
            .ToListAsync(cancellationToken);

        // Sort in memory - SQLite doesn't support DateTimeOffset in ORDER BY
        return tasks.OrderByDescending(t => t.CreatedAt).ToList();
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
