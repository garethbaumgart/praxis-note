namespace PraxisNote.Domain.Aggregates.Users;

public interface ILinkedIdentityRepository
{
    Task<LinkedIdentity?> GetByProviderAsync(string provider, string providerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LinkedIdentity>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(LinkedIdentity identity, CancellationToken cancellationToken = default);
    void Remove(LinkedIdentity identity);
}
