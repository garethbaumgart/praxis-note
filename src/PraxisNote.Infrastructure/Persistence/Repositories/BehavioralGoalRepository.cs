using Microsoft.EntityFrameworkCore;
using PraxisNote.Domain.Aggregates.BehavioralGoals;

namespace PraxisNote.Infrastructure.Persistence.Repositories;

public sealed class BehavioralGoalRepository(PraxisNoteDbContext context) : IBehavioralGoalRepository
{
    public async Task<BehavioralGoal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.BehavioralGoals.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<BehavioralGoal>> GetByUserIdAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default)
    {
        return await context.BehavioralGoals
            .Where(g => g.UserId == userId && g.ProfileId == profileId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(BehavioralGoal goal, CancellationToken cancellationToken = default)
    {
        await context.BehavioralGoals.AddAsync(goal, cancellationToken);
    }

    public void Remove(BehavioralGoal goal)
    {
        context.BehavioralGoals.Remove(goal);
    }
}
