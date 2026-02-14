namespace PraxisNote.Domain.Aggregates.BlindSpotNudges;

public interface IBlindSpotNudgeRepository
{
    Task<IReadOnlyList<BlindSpotNudge>> GetActiveByUserAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BlindSpotNudge>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<BlindSpotNudge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<BlindSpotNudge> nudges, CancellationToken cancellationToken = default);
    void Remove(BlindSpotNudge nudge);
}
