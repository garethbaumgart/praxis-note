namespace PraxisNote.Domain.Aggregates.Tags;

public interface ITagRepository
{
    Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tag>> GetByUserIdAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tag>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Tag?> GetByNameAsync(Guid userId, Guid profileId, string name, CancellationToken cancellationToken = default);
    Task<bool> ExistsByProfileAsync(Guid userId, Guid profileId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tag>> GetByNamesAsync(Guid userId, Guid profileId, IEnumerable<string> names, CancellationToken cancellationToken = default);
    Task AddAsync(Tag tag, CancellationToken cancellationToken = default);
    void Remove(Tag tag);
}
