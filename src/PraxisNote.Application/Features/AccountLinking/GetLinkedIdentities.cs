using PraxisNote.Domain.Aggregates.Users;

namespace PraxisNote.Application.Features.AccountLinking;

public sealed class GetLinkedIdentities(ILinkedIdentityRepository linkedIdentityRepository)
{
    public record Query(Guid UserId);

    public async Task<IReadOnlyList<LinkedIdentityDto>> ExecuteAsync(
        Query query,
        CancellationToken cancellationToken = default)
    {
        var identities = await linkedIdentityRepository.GetByUserIdAsync(query.UserId, cancellationToken);

        return identities.Select(li => new LinkedIdentityDto(
            li.Id,
            li.Provider,
            li.Email,
            li.Name,
            li.AvatarUrl,
            li.DefaultProfileId,
            li.LinkedAt)).ToList();
    }
}
