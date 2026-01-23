namespace PraxisNote.Domain.Aggregates.Tags;

public interface ITagRepository
{
    Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tag>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Tag?> GetByNameAsync(Guid userId, string name, CancellationToken cancellationToken = default);
    Task AddAsync(Tag tag, CancellationToken cancellationToken = default);
    void Remove(Tag tag);
}
