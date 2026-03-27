namespace PraxisNote.Domain.Aggregates.UserAiKeys;

public interface IUserAiKeyRepository
{
    Task<UserAiKey?> GetByUserAndProviderAsync(Guid userId, AiProvider provider, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserAiKey>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserAiKey key, CancellationToken cancellationToken = default);
    Task<UserAiKey> UpsertAsync(Guid userId, AiProvider provider, string encryptedKey, string keyHint, string? preferredModel, CancellationToken cancellationToken = default);
    void Remove(UserAiKey key);
}
