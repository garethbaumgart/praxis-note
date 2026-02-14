namespace PraxisNote.Domain.Aggregates.Notes;

public interface INoteRepository
{
    Task<Note?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Note>> GetByUserIdAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Note>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Note>> GetByTagIdAsync(Guid userId, Guid profileId, Guid tagId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, int>> GetTagUsageCountsAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByProfileAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default);
    Task AddAsync(Note note, CancellationToken cancellationToken = default);
    void Remove(Note note);
}
