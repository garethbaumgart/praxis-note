namespace PraxisNote.Domain.Aggregates.Meetings;

public interface IMeetingRepository
{
    Task<Meeting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Meeting>> GetByUserIdAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Meeting>> GetByTagIdAsync(Guid userId, Guid profileId, Guid tagId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, int>> GetTagUsageCountsAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default);
    Task AddAsync(Meeting meeting, CancellationToken cancellationToken = default);
    void Remove(Meeting meeting);
    Task<HashSet<string>> GetExistingCalendarEventIdsAsync(Guid userId, Guid profileId, IEnumerable<string> eventIds, CancellationToken cancellationToken = default);
}
