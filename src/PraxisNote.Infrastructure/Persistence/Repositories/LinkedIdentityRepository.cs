using Microsoft.EntityFrameworkCore;
using PraxisNote.Domain.Aggregates.Users;

namespace PraxisNote.Infrastructure.Persistence.Repositories;

public sealed class LinkedIdentityRepository(PraxisNoteDbContext context) : ILinkedIdentityRepository
{
    public async Task<LinkedIdentity?> GetByProviderAsync(
        string provider,
        string providerId,
        CancellationToken cancellationToken = default)
    {
        return await context.LinkedIdentities
            .FirstOrDefaultAsync(
                li => li.Provider == provider && li.ProviderId == providerId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<LinkedIdentity>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await context.LinkedIdentities
            .Where(li => li.UserId == userId)
            .OrderBy(li => li.LinkedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await context.LinkedIdentities
            .CountAsync(li => li.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(LinkedIdentity identity, CancellationToken cancellationToken = default)
    {
        await context.LinkedIdentities.AddAsync(identity, cancellationToken);
    }

    public void Remove(LinkedIdentity identity)
    {
        context.LinkedIdentities.Remove(identity);
    }
}
