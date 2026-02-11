using Microsoft.EntityFrameworkCore;
using PraxisNote.Domain.Aggregates.Profiles;

namespace PraxisNote.Infrastructure.Persistence.Repositories;

public sealed class ProfileRepository(PraxisNoteDbContext context) : IProfileRepository
{
    public async Task<Profile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Profiles.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<Profile>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Profiles
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Profile?> GetDefaultByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Profiles
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsDefault, cancellationToken);
    }

    public async Task<int> GetCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Profiles
            .CountAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        await context.Profiles.AddAsync(profile, cancellationToken);
    }

    public void Remove(Profile profile)
    {
        context.Profiles.Remove(profile);
    }
}
