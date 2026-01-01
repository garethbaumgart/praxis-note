using Microsoft.EntityFrameworkCore;
using PraxisNote.Domain.Aggregates.Users;
using PraxisNote.Domain.ValueObjects;
using PraxisNote.Infrastructure.Application.Abstractions;

namespace PraxisNote.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(PraxisNoteDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Users.FindAsync([id], cancellationToken);
    }

    public async Task<User?> GetByExternalIdentityAsync(
        ExternalIdentity externalIdentity,
        CancellationToken cancellationToken = default)
    {
        return await context.Users
            .FirstOrDefaultAsync(u =>
                u.ExternalIdentity.Provider == externalIdentity.Provider &&
                u.ExternalIdentity.ProviderId == externalIdentity.ProviderId,
                cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await context.Users.AddAsync(user, cancellationToken);
    }

    public void Update(User user)
    {
        context.Users.Update(user);
    }
}
