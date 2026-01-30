using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.BehavioralGoals;

namespace PraxisNote.Application.Features.Goals;

public sealed class UpdateBehavioralGoal(IBehavioralGoalRepository goalRepository, IUnitOfWork unitOfWork)
{
    public record Command(
        Guid UserId,
        Guid GoalId,
        MetricType MetricType,
        GoalOperator Operator,
        double TargetValue,
        double? TargetValueUpper,
        string Title,
        bool IsActive);

    public const string NotFoundError = "GOAL_NOT_FOUND";

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var goal = await goalRepository.GetByIdAsync(command.GoalId, cancellationToken);
        if (goal is null || goal.UserId != command.UserId)
        {
            throw new InvalidOperationException(NotFoundError);
        }

        goal.Update(
            command.MetricType,
            command.Operator,
            command.TargetValue,
            command.TargetValueUpper,
            command.Title);

        if (command.IsActive)
            goal.Activate();
        else
            goal.Deactivate();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
