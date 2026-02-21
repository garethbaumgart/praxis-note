namespace PraxisNote.Domain.Aggregates.ArchetypeSnapshots;

public sealed class ArchetypeSnapshot
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ProfileId { get; private set; }
    public DateOnly WeekStartDate { get; private set; }
    public string PrimaryArchetype { get; private set; } = string.Empty;
    public double Score { get; private set; }
    public int MeetingCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ArchetypeSnapshot() { }

    public static ArchetypeSnapshot Create(
        Guid userId,
        Guid profileId,
        DateOnly weekStartDate,
        string primaryArchetype,
        double score,
        int meetingCount)
    {
        ArgumentNullException.ThrowIfNull(primaryArchetype);
        if (score < 0 || score > 100)
            throw new ArgumentException("Score must be between 0 and 100", nameof(score));
        if (meetingCount < 0)
            throw new ArgumentException("Meeting count cannot be negative", nameof(meetingCount));

        return new ArchetypeSnapshot
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProfileId = profileId,
            WeekStartDate = weekStartDate,
            PrimaryArchetype = primaryArchetype,
            Score = score,
            MeetingCount = meetingCount,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
