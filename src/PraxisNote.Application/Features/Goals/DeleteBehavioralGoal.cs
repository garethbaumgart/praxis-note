using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.BehavioralGoals;

namespace PraxisNote.Application.Features.Goals;

public sealed class DeleteBehavioralGoal(IBehavioralGoalRepository goalRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid GoalId);

    public const string NotFoundError = "GOAL_NOT_FOUND";

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var goal = await goalRepository.GetByIdAsync(command.GoalId, cancellationToken);
        if (goal is null || goal.UserId != command.UserId)
        {
            throw new InvalidOperationException(NotFoundError);
        }

        goalRepository.Remove(goal);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
