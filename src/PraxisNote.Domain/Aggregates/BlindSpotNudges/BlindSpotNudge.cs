using PraxisNote.Domain.Common;

namespace PraxisNote.Domain.Aggregates.BlindSpotNudges;

public sealed class BlindSpotNudge : AggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid ProfileId { get; private set; }
    public string Dimension { get; private set; } = string.Empty;
    public string Suggestion { get; private set; } = string.Empty;
    public string BlindSpotDescription { get; private set; } = string.Empty;
    public NudgeStatus Status { get; private set; }
    public Guid? ConvertedGoalId { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private BlindSpotNudge() { }

    private BlindSpotNudge(
        Guid id,
        Guid userId,
        Guid profileId,
        string dimension,
        string suggestion,
        string blindSpotDescription) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty, nameof(userId));
        ArgumentOutOfRangeException.ThrowIfEqual(profileId, Guid.Empty, nameof(profileId));
        ArgumentException.ThrowIfNullOrWhiteSpace(dimension);
        ArgumentException.ThrowIfNullOrWhiteSpace(suggestion);
        ArgumentException.ThrowIfNullOrWhiteSpace(blindSpotDescription);

        UserId = userId;
        ProfileId = profileId;
        Dimension = dimension;
        Suggestion = suggestion;
        BlindSpotDescription = blindSpotDescription;
        Status = NudgeStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static BlindSpotNudge Create(
        Guid userId,
        Guid profileId,
        string dimension,
        string suggestion,
        string blindSpotDescription)
    {
        return new BlindSpotNudge(Guid.NewGuid(), userId, profileId, dimension, suggestion, blindSpotDescription);
    }

    public void Dismiss()
    {
        if (Status != NudgeStatus.Active)
            throw new InvalidOperationException("Only active nudges can be dismissed.");

        Status = NudgeStatus.Dismissed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AcceptAsGoal(Guid goalId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(goalId, Guid.Empty, nameof(goalId));

        if (Status != NudgeStatus.Active)
            throw new InvalidOperationException("Only active nudges can be accepted as goals.");

        Status = NudgeStatus.AcceptedAsGoal;
        ConvertedGoalId = goalId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Reassigns this blind spot nudge to a different user and profile.
    /// Used during account linking to transfer data before deleting the source user.
    /// </summary>
    public void Reassign(Guid newUserId, Guid newProfileId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(newUserId, Guid.Empty, nameof(newUserId));
        ArgumentOutOfRangeException.ThrowIfEqual(newProfileId, Guid.Empty, nameof(newProfileId));

        UserId = newUserId;
        ProfileId = newProfileId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
