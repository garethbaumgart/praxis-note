namespace PraxisNote.Domain.Aggregates.Profiles;

public interface IProfileRepository
{
    Task<Profile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Profile>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Profile?> GetDefaultByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Profile profile, CancellationToken cancellationToken = default);
    void Remove(Profile profile);
}
