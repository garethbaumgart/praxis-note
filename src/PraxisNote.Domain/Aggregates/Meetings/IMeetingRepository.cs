namespace PraxisNote.Domain.Aggregates.Meetings;

public interface IMeetingRepository
{
    Task<Meeting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Meeting>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Meeting>> GetByTagIdAsync(Guid userId, Guid tagId, CancellationToken cancellationToken = default);
    Task AddAsync(Meeting meeting, CancellationToken cancellationToken = default);
    void Remove(Meeting meeting);
    Task<HashSet<string>> GetExistingCalendarEventIdsAsync(Guid userId, IEnumerable<string> eventIds, CancellationToken cancellationToken = default);
}
