using PraxisNote.Domain.Aggregates.BehavioralGoals;

namespace PraxisNote.Application.Features.Goals;

public sealed class GetUserGoals(IBehavioralGoalRepository goalRepository)
{
    public record Query(Guid UserId);

    public async Task<IReadOnlyList<BehavioralGoalDto>> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var goals = await goalRepository.GetByUserIdAsync(query.UserId, cancellationToken);

        return goals
            .Select(g => new BehavioralGoalDto(
                g.Id,
                g.MetricType.ToString(),
                g.Operator.ToString(),
                g.TargetValue,
                g.TargetValueUpper,
                g.Title,
                g.IsActive,
                g.CreatedAt))
            .ToList();
    }
}

public record BehavioralGoalDto(
    Guid Id,
    string MetricType,
    string Operator,
    double TargetValue,
    double? TargetValueUpper,
    string Title,
    bool IsActive,
    DateTimeOffset CreatedAt);
