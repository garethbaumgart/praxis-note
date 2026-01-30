using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.BehavioralGoals;

namespace PraxisNote.Application.Features.Goals;

public sealed class CreateBehavioralGoal(IBehavioralGoalRepository goalRepository, IUnitOfWork unitOfWork)
{
    public record Command(
        Guid UserId,
        MetricType MetricType,
        GoalOperator Operator,
        double TargetValue,
        double? TargetValueUpper,
        string Title);

    public record Result(Guid GoalId);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var goal = BehavioralGoal.Create(
            command.UserId,
            command.MetricType,
            command.Operator,
            command.TargetValue,
            command.TargetValueUpper,
            command.Title);

        await goalRepository.AddAsync(goal, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new Result(goal.Id);
    }
}
