namespace PraxisNote.Domain.Aggregates.BehavioralGoals;

public interface IBehavioralGoalRepository
{
    Task<BehavioralGoal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BehavioralGoal>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(BehavioralGoal goal, CancellationToken cancellationToken = default);
    void Remove(BehavioralGoal goal);
}
