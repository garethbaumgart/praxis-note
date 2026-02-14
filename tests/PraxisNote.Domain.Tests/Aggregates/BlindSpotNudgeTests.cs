using PraxisNote.Domain.Aggregates.BlindSpotNudges;

namespace PraxisNote.Domain.Tests.Aggregates;

public class BlindSpotNudgeTests
{
    private readonly Guid _validUserId = Guid.NewGuid();
    private readonly Guid _validProfileId = Guid.NewGuid();

    #region Create

    [Fact]
    public void Create_WithValidInputs_SetsProperties()
    {
        var nudge = BlindSpotNudge.Create(
            _validUserId, _validProfileId, "Talk Time",
            "Try the 60-second rule", "You estimated ~30%, AI measured 65%");

        Assert.NotEqual(Guid.Empty, nudge.Id);
        Assert.Equal(_validUserId, nudge.UserId);
        Assert.Equal(_validProfileId, nudge.ProfileId);
        Assert.Equal("Talk Time", nudge.Dimension);
        Assert.Equal("Try the 60-second rule", nudge.Suggestion);
        Assert.Equal("You estimated ~30%, AI measured 65%", nudge.BlindSpotDescription);
        Assert.Equal(NudgeStatus.Active, nudge.Status);
        Assert.Null(nudge.ConvertedGoalId);
        Assert.True(nudge.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Equal(nudge.CreatedAt, nudge.UpdatedAt);
    }

    [Fact]
    public void Create_WithEmptyUserId_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BlindSpotNudge.Create(Guid.Empty, _validProfileId, "Talk Time",
                "suggestion", "description"));
    }

    [Fact]
    public void Create_WithEmptyProfileId_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BlindSpotNudge.Create(_validUserId, Guid.Empty, "Talk Time",
                "suggestion", "description"));
    }

    [Fact]
    public void Create_WithNullDimension_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            BlindSpotNudge.Create(_validUserId, _validProfileId, null!,
                "suggestion", "description"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceDimension_ThrowsArgumentException(string invalidDimension)
    {
        Assert.Throws<ArgumentException>(() =>
            BlindSpotNudge.Create(_validUserId, _validProfileId, invalidDimension,
                "suggestion", "description"));
    }

    [Fact]
    public void Create_WithNullSuggestion_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            BlindSpotNudge.Create(_validUserId, _validProfileId, "Talk Time",
                null!, "description"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceSuggestion_ThrowsArgumentException(string invalidSuggestion)
    {
        Assert.Throws<ArgumentException>(() =>
            BlindSpotNudge.Create(_validUserId, _validProfileId, "Talk Time",
                invalidSuggestion, "description"));
    }

    [Fact]
    public void Create_WithNullBlindSpotDescription_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            BlindSpotNudge.Create(_validUserId, _validProfileId, "Talk Time",
                "suggestion", null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceBlindSpotDescription_ThrowsArgumentException(string invalidDescription)
    {
        Assert.Throws<ArgumentException>(() =>
            BlindSpotNudge.Create(_validUserId, _validProfileId, "Talk Time",
                "suggestion", invalidDescription));
    }

    #endregion

    #region Dismiss

    [Fact]
    public void Dismiss_SetsStatusToDismissed()
    {
        var nudge = BlindSpotNudge.Create(
            _validUserId, _validProfileId, "Talk Time",
            "suggestion", "description");

        nudge.Dismiss();

        Assert.Equal(NudgeStatus.Dismissed, nudge.Status);
    }

    [Fact]
    public void Dismiss_UpdatesTimestamp()
    {
        var nudge = BlindSpotNudge.Create(
            _validUserId, _validProfileId, "Talk Time",
            "suggestion", "description");
        var originalUpdatedAt = nudge.UpdatedAt;

        nudge.Dismiss();

        Assert.True(nudge.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void Dismiss_WhenAlreadyDismissed_Throws()
    {
        var nudge = BlindSpotNudge.Create(
            _validUserId, _validProfileId, "Talk Time",
            "suggestion", "description");
        nudge.Dismiss();

        Assert.Throws<InvalidOperationException>(() => nudge.Dismiss());
    }

    [Fact]
    public void Dismiss_WhenAlreadyAccepted_Throws()
    {
        var nudge = BlindSpotNudge.Create(
            _validUserId, _validProfileId, "Talk Time",
            "suggestion", "description");
        nudge.AcceptAsGoal(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => nudge.Dismiss());
    }

    #endregion

    #region AcceptAsGoal

    [Fact]
    public void AcceptAsGoal_SetsStatusAndGoalId()
    {
        var nudge = BlindSpotNudge.Create(
            _validUserId, _validProfileId, "Talk Time",
            "suggestion", "description");
        var goalId = Guid.NewGuid();

        nudge.AcceptAsGoal(goalId);

        Assert.Equal(NudgeStatus.AcceptedAsGoal, nudge.Status);
        Assert.Equal(goalId, nudge.ConvertedGoalId);
    }

    [Fact]
    public void AcceptAsGoal_UpdatesTimestamp()
    {
        var nudge = BlindSpotNudge.Create(
            _validUserId, _validProfileId, "Talk Time",
            "suggestion", "description");
        var originalUpdatedAt = nudge.UpdatedAt;

        nudge.AcceptAsGoal(Guid.NewGuid());

        Assert.True(nudge.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void AcceptAsGoal_WithEmptyGoalId_Throws()
    {
        var nudge = BlindSpotNudge.Create(
            _validUserId, _validProfileId, "Talk Time",
            "suggestion", "description");

        Assert.Throws<ArgumentOutOfRangeException>(() => nudge.AcceptAsGoal(Guid.Empty));
    }

    [Fact]
    public void AcceptAsGoal_WhenAlreadyDismissed_Throws()
    {
        var nudge = BlindSpotNudge.Create(
            _validUserId, _validProfileId, "Talk Time",
            "suggestion", "description");
        nudge.Dismiss();

        Assert.Throws<InvalidOperationException>(() => nudge.AcceptAsGoal(Guid.NewGuid()));
    }

    [Fact]
    public void AcceptAsGoal_WhenAlreadyAccepted_Throws()
    {
        var nudge = BlindSpotNudge.Create(
            _validUserId, _validProfileId, "Talk Time",
            "suggestion", "description");
        nudge.AcceptAsGoal(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => nudge.AcceptAsGoal(Guid.NewGuid()));
    }

    #endregion

    #region Reassign Tests

    [Fact]
    public void Reassign_WithValidIds_UpdatesUserIdAndProfileId()
    {
        // Arrange
        var nudge = BlindSpotNudge.Create(
            _validUserId, _validProfileId, "Talk Time",
            "suggestion", "description");
        var newUserId = Guid.NewGuid();
        var newProfileId = Guid.NewGuid();

        // Act
        nudge.Reassign(newUserId, newProfileId);

        // Assert
        Assert.Equal(newUserId, nudge.UserId);
        Assert.Equal(newProfileId, nudge.ProfileId);
    }

    [Fact]
    public void Reassign_UpdatesUpdatedAt()
    {
        // Arrange
        var nudge = BlindSpotNudge.Create(
            _validUserId, _validProfileId, "Talk Time",
            "suggestion", "description");
        var originalUpdatedAt = nudge.UpdatedAt;

        // Act
        nudge.Reassign(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        Assert.True(nudge.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void Reassign_WithEmptyUserId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var nudge = BlindSpotNudge.Create(
            _validUserId, _validProfileId, "Talk Time",
            "suggestion", "description");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            nudge.Reassign(Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void Reassign_WithEmptyProfileId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var nudge = BlindSpotNudge.Create(
            _validUserId, _validProfileId, "Talk Time",
            "suggestion", "description");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            nudge.Reassign(Guid.NewGuid(), Guid.Empty));
    }

    #endregion
}
