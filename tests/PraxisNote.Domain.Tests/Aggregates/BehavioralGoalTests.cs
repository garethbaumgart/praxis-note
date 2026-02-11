using PraxisNote.Domain.Aggregates.BehavioralGoals;

namespace PraxisNote.Domain.Tests.Aggregates;

public class BehavioralGoalTests
{
    private readonly Guid _validUserId = Guid.NewGuid();
    private readonly Guid _validProfileId = Guid.NewGuid();

    #region Create

    [Fact]
    public void Create_WithValidInputs_ReturnsGoalWithCorrectProperties()
    {
        var goal = BehavioralGoal.Create(
            _validUserId, _validProfileId, MetricType.TalkTimePercentage, GoalOperator.LessThan,
            50, null, "Keep talk time under 50%");

        Assert.NotEqual(Guid.Empty, goal.Id);
        Assert.Equal(_validUserId, goal.UserId);
        Assert.Equal(MetricType.TalkTimePercentage, goal.MetricType);
        Assert.Equal(GoalOperator.LessThan, goal.Operator);
        Assert.Equal(50, goal.TargetValue);
        Assert.Null(goal.TargetValueUpper);
        Assert.Equal("Keep talk time under 50%", goal.Title);
        Assert.True(goal.IsActive);
        Assert.True(goal.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Equal(goal.CreatedAt, goal.UpdatedAt);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BehavioralGoal.Create(Guid.Empty, _validProfileId, MetricType.TalkTimePercentage, GoalOperator.LessThan, 50, null, "Test"));
    }

    [Fact]
    public void Create_WithNullTitle_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            BehavioralGoal.Create(_validUserId, _validProfileId, MetricType.TalkTimePercentage, GoalOperator.LessThan, 50, null, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceTitle_ThrowsArgumentException(string invalidTitle)
    {
        Assert.Throws<ArgumentException>(() =>
            BehavioralGoal.Create(_validUserId, _validProfileId, MetricType.TalkTimePercentage, GoalOperator.LessThan, 50, null, invalidTitle));
    }

    [Fact]
    public void Create_BetweenOperatorWithoutUpperValue_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            BehavioralGoal.Create(_validUserId, _validProfileId, MetricType.TalkTimePercentage, GoalOperator.Between, 30, null, "Test"));
    }

    [Fact]
    public void Create_BetweenOperatorWithUpperLessThanLower_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            BehavioralGoal.Create(_validUserId, _validProfileId, MetricType.TalkTimePercentage, GoalOperator.Between, 50, 30, "Test"));
    }

    [Fact]
    public void Create_BetweenOperatorWithValidRange_Succeeds()
    {
        var goal = BehavioralGoal.Create(
            _validUserId, _validProfileId, MetricType.TalkTimePercentage, GoalOperator.Between,
            30, 50, "Talk time between 30-50%");

        Assert.Equal(GoalOperator.Between, goal.Operator);
        Assert.Equal(30, goal.TargetValue);
        Assert.Equal(50, goal.TargetValueUpper);
    }

    #endregion

    #region Update

    [Fact]
    public void Update_WithValidInputs_UpdatesProperties()
    {
        var goal = BehavioralGoal.Create(
            _validUserId, _validProfileId, MetricType.TalkTimePercentage, GoalOperator.LessThan, 50, null, "Old title");

        goal.Update(MetricType.QuestionRatio, GoalOperator.GreaterThanOrEqual, 0.3, null, "New title");

        Assert.Equal(MetricType.QuestionRatio, goal.MetricType);
        Assert.Equal(GoalOperator.GreaterThanOrEqual, goal.Operator);
        Assert.Equal(0.3, goal.TargetValue);
        Assert.Equal("New title", goal.Title);
        Assert.True(goal.UpdatedAt >= goal.CreatedAt);
    }

    [Fact]
    public void Update_WithEmptyTitle_ThrowsArgumentException()
    {
        var goal = BehavioralGoal.Create(
            _validUserId, _validProfileId, MetricType.TalkTimePercentage, GoalOperator.LessThan, 50, null, "Valid title");

        Assert.Throws<ArgumentException>(() =>
            goal.Update(MetricType.TalkTimePercentage, GoalOperator.LessThan, 50, null, ""));
    }

    #endregion

    #region Activate / Deactivate

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        var goal = BehavioralGoal.Create(
            _validUserId, _validProfileId, MetricType.TalkTimePercentage, GoalOperator.LessThan, 50, null, "Test");

        goal.Deactivate();

        Assert.False(goal.IsActive);
    }

    [Fact]
    public void Activate_AfterDeactivate_SetsIsActiveToTrue()
    {
        var goal = BehavioralGoal.Create(
            _validUserId, _validProfileId, MetricType.TalkTimePercentage, GoalOperator.LessThan, 50, null, "Test");

        goal.Deactivate();
        goal.Activate();

        Assert.True(goal.IsActive);
    }

    #endregion

    #region Evaluate

    [Theory]
    [InlineData(GoalOperator.LessThan, 50, 40, true)]
    [InlineData(GoalOperator.LessThan, 50, 50, false)]
    [InlineData(GoalOperator.LessThan, 50, 60, false)]
    [InlineData(GoalOperator.LessThanOrEqual, 50, 50, true)]
    [InlineData(GoalOperator.LessThanOrEqual, 50, 51, false)]
    [InlineData(GoalOperator.GreaterThan, 0.3, 0.4, true)]
    [InlineData(GoalOperator.GreaterThan, 0.3, 0.3, false)]
    [InlineData(GoalOperator.GreaterThanOrEqual, 0.3, 0.3, true)]
    [InlineData(GoalOperator.GreaterThanOrEqual, 0.3, 0.2, false)]
    public void Evaluate_WithSimpleOperator_ReturnsExpectedResult(
        GoalOperator op, double target, double actual, bool expected)
    {
        var goal = BehavioralGoal.Create(
            _validUserId, _validProfileId, MetricType.TalkTimePercentage, op, target, null, "Test");

        Assert.Equal(expected, goal.Evaluate(actual));
    }

    [Theory]
    [InlineData(30, 50, 40, true)]
    [InlineData(30, 50, 30, true)]
    [InlineData(30, 50, 50, true)]
    [InlineData(30, 50, 29, false)]
    [InlineData(30, 50, 51, false)]
    public void Evaluate_WithBetweenOperator_ReturnsExpectedResult(
        double lower, double upper, double actual, bool expected)
    {
        var goal = BehavioralGoal.Create(
            _validUserId, _validProfileId, MetricType.TalkTimePercentage, GoalOperator.Between,
            lower, upper, "Test");

        Assert.Equal(expected, goal.Evaluate(actual));
    }

    #endregion
}
