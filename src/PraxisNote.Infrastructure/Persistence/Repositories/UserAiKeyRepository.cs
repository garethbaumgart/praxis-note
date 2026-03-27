using Microsoft.EntityFrameworkCore;
using PraxisNote.Domain.Aggregates.UserAiKeys;

namespace PraxisNote.Infrastructure.Persistence.Repositories;

public sealed class UserAiKeyRepository(PraxisNoteDbContext context) : IUserAiKeyRepository
{
    public async Task<UserAiKey?> GetByUserAndProviderAsync(Guid userId, AiProvider provider, CancellationToken cancellationToken = default)
        => await context.UserAiKeys.FirstOrDefaultAsync(k => k.UserId == userId && k.Provider == provider, cancellationToken);

    public async Task<IReadOnlyList<UserAiKey>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.UserAiKeys.Where(k => k.UserId == userId).OrderBy(k => k.Provider).ToListAsync(cancellationToken);

    public async Task AddAsync(UserAiKey key, CancellationToken cancellationToken = default)
        => await context.UserAiKeys.AddAsync(key, cancellationToken);

    public void Remove(UserAiKey key)
        => context.UserAiKeys.Remove(key);
}
