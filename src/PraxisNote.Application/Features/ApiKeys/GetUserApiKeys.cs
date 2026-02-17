using PraxisNote.Domain.Aggregates.ApiKeys;

namespace PraxisNote.Application.Features.ApiKeys;

public sealed class GetUserApiKeys(IApiKeyRepository apiKeyRepository)
{
    public record Query(Guid UserId);

    public async Task<IReadOnlyList<ApiKeyDto>> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var keys = await apiKeyRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        return keys.Select(k => new ApiKeyDto(
            k.Id, k.Name, k.KeyPrefix, k.ProfileId,
            k.CreatedAt, k.LastUsedAt, k.ExpiresAt, k.IsRevoked)).ToList();
    }
}
