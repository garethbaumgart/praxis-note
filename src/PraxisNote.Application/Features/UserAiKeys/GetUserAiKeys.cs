using PraxisNote.Domain.Aggregates.UserAiKeys;

namespace PraxisNote.Application.Features.UserAiKeys;

public sealed class GetUserAiKeys(IUserAiKeyRepository repository)
{
    public record Query(Guid UserId);

    public async Task<IReadOnlyList<UserAiKeyDto>> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var keys = await repository.GetByUserIdAsync(query.UserId, cancellationToken);

        return keys
            .Select(k => new UserAiKeyDto(k.Provider.ToString(), true, k.KeyHint, k.PreferredModel, k.CreatedAt))
            .ToList();
    }
}
