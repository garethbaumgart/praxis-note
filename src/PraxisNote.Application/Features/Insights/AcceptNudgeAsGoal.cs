using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Goals;
using PraxisNote.Domain.Aggregates.BehavioralGoals;
using PraxisNote.Domain.Aggregates.BlindSpotNudges;

namespace PraxisNote.Application.Features.Insights;

public sealed class AcceptNudgeAsGoal(
    IBlindSpotNudgeRepository nudgeRepository,
    CreateBehavioralGoal createGoal,
    IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid ProfileId, Guid NudgeId);

    public const string NotFoundError = "NUDGE_NOT_FOUND";

    public async Task<Guid> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var nudge = await nudgeRepository.GetByIdAsync(command.NudgeId, cancellationToken);
        if (nudge is null || nudge.UserId != command.UserId)
        {
            throw new InvalidOperationException(NotFoundError);
        }

        // Map dimension to goal parameters
        var (metricType, goalOperator, targetValue, title) = MapDimensionToGoal(nudge.Dimension, nudge.Suggestion);

        var goalCommand = new CreateBehavioralGoal.Command(
            command.UserId,
            command.ProfileId,
            metricType,
            goalOperator,
            targetValue,
            null,
            title);

        var goalResult = await createGoal.ExecuteAsync(goalCommand, cancellationToken);

        nudge.AcceptAsGoal(goalResult.GoalId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return goalResult.GoalId;
    }

    private static (MetricType MetricType, GoalOperator Operator, double TargetValue, string Title) MapDimensionToGoal(
        string dimension, string suggestion)
    {
        return dimension switch
        {
            "Talk Time" => (MetricType.TalkTimePercentage, GoalOperator.LessThan, 50, "Keep talk time under 50%"),
            "Interruptions" => (MetricType.InterruptionCount, GoalOperator.LessThanOrEqual, 2, "Limit interruptions to 2 or fewer"),
            "Tone" => (MetricType.SentimentScore, GoalOperator.GreaterThanOrEqual, 0.6, "Maintain positive tone (sentiment >= 0.6)"),
            "Engagement" => (MetricType.QuestionRatio, GoalOperator.GreaterThanOrEqual, 0.3, "Increase engagement through questions"),
            _ => (MetricType.SentimentScore, GoalOperator.GreaterThanOrEqual, 0.5, suggestion.Length > 100 ? suggestion[..100] : suggestion)
        };
    }
}
