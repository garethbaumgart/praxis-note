using PraxisNote.Domain.Aggregates.Profiles;

namespace PraxisNote.Application.Features.Profiles;

public sealed class GetUserProfiles(IProfileRepository profileRepository)
{
    public record Query(Guid UserId);

    public record ProfileDto(Guid Id, string Name, string? Icon, bool IsDefault, DateTimeOffset CreatedAt);

    public async Task<IReadOnlyList<ProfileDto>> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var profiles = await profileRepository.GetByUserIdAsync(query.UserId, cancellationToken);

        return profiles
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.CreatedAt)
            .Select(p => new ProfileDto(p.Id, p.Name, p.Icon, p.IsDefault, p.CreatedAt))
            .ToList();
    }
}
