namespace PraxisNote.Domain.Aggregates.ApiKeys;

public interface IApiKeyRepository
{
    Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApiKey>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(ApiKey apiKey, CancellationToken cancellationToken = default);
}
