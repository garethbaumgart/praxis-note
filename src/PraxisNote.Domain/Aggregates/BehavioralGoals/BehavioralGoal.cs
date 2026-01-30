using PraxisNote.Domain.Common;

namespace PraxisNote.Domain.Aggregates.BehavioralGoals;

public sealed class BehavioralGoal : AggregateRoot
{
    public Guid UserId { get; private init; }
    public MetricType MetricType { get; private set; }
    public GoalOperator Operator { get; private set; }
    public double TargetValue { get; private set; }
    public double? TargetValueUpper { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private BehavioralGoal() { }

    private BehavioralGoal(
        Guid id,
        Guid userId,
        MetricType metricType,
        GoalOperator goalOperator,
        double targetValue,
        double? targetValueUpper,
        string title) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty, nameof(userId));
        ValidateTitle(title);
        ValidateTarget(goalOperator, targetValue, targetValueUpper);

        UserId = userId;
        MetricType = metricType;
        Operator = goalOperator;
        TargetValue = targetValue;
        TargetValueUpper = targetValueUpper;
        Title = title;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static BehavioralGoal Create(
        Guid userId,
        MetricType metricType,
        GoalOperator goalOperator,
        double targetValue,
        double? targetValueUpper,
        string title)
    {
        return new BehavioralGoal(Guid.NewGuid(), userId, metricType, goalOperator, targetValue, targetValueUpper, title);
    }

    public void Update(
        MetricType metricType,
        GoalOperator goalOperator,
        double targetValue,
        double? targetValueUpper,
        string title)
    {
        ValidateTitle(title);
        ValidateTarget(goalOperator, targetValue, targetValueUpper);

        MetricType = metricType;
        Operator = goalOperator;
        TargetValue = targetValue;
        TargetValueUpper = targetValueUpper;
        Title = title;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool Evaluate(double actualValue)
    {
        return Operator switch
        {
            GoalOperator.LessThan => actualValue < TargetValue,
            GoalOperator.LessThanOrEqual => actualValue <= TargetValue,
            GoalOperator.GreaterThan => actualValue > TargetValue,
            GoalOperator.GreaterThanOrEqual => actualValue >= TargetValue,
            GoalOperator.Between => TargetValueUpper.HasValue
                                    && actualValue >= TargetValue
                                    && actualValue <= TargetValueUpper.Value,
            _ => false
        };
    }

    private static void ValidateTitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
    }

    private static void ValidateTarget(GoalOperator goalOperator, double targetValue, double? targetValueUpper)
    {
        if (goalOperator == GoalOperator.Between)
        {
            if (!targetValueUpper.HasValue)
                throw new ArgumentException("TargetValueUpper is required when operator is Between.", nameof(targetValueUpper));

            if (targetValueUpper.Value <= targetValue)
                throw new ArgumentException("TargetValueUpper must be greater than TargetValue.", nameof(targetValueUpper));
        }
    }
}
