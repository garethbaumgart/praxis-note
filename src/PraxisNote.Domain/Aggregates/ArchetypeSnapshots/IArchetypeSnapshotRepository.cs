namespace PraxisNote.Domain.Aggregates.ArchetypeSnapshots;

public interface IArchetypeSnapshotRepository
{
    Task<IReadOnlyList<ArchetypeSnapshot>> GetByUserIdAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default);
    Task<ArchetypeSnapshot?> GetByWeekAsync(Guid userId, Guid profileId, DateOnly weekStartDate, CancellationToken cancellationToken = default);
    Task AddAsync(ArchetypeSnapshot snapshot, CancellationToken cancellationToken = default);
}
