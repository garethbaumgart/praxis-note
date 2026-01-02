using PraxisNote.Domain.ValueObjects;

namespace PraxisNote.Domain.Aggregates.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByExternalIdentityAsync(ExternalIdentity externalIdentity, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    void Update(User user);
}
