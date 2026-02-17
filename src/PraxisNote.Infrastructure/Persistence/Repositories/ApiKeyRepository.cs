using Microsoft.EntityFrameworkCore;
using PraxisNote.Domain.Aggregates.ApiKeys;

namespace PraxisNote.Infrastructure.Persistence.Repositories;

public sealed class ApiKeyRepository(PraxisNoteDbContext context) : IApiKeyRepository
{
    public async Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default)
        => await context.ApiKeys.FirstOrDefaultAsync(k => k.KeyHash == keyHash, cancellationToken);

    public async Task<IReadOnlyList<ApiKey>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.ApiKeys.Where(k => k.UserId == userId).OrderByDescending(k => k.CreatedAt).ToListAsync(cancellationToken);

    public async Task AddAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
        => await context.ApiKeys.AddAsync(apiKey, cancellationToken);
}
