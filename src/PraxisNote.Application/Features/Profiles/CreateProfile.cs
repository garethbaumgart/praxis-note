using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Profiles;

namespace PraxisNote.Application.Features.Profiles;

public sealed class CreateProfile(
    IProfileRepository profileRepository,
    IUnitOfWork unitOfWork)
{
    public const int MaxProfilesPerUser = 5;
    public const string MaxProfilesError = "Maximum number of profiles reached";

    public record Command(Guid UserId, string Name, string? Icon = null);
    public record Result(Guid ProfileId);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var count = await profileRepository.GetCountByUserIdAsync(command.UserId, cancellationToken);
        if (count >= MaxProfilesPerUser)
        {
            throw new InvalidOperationException(MaxProfilesError);
        }

        var profile = Profile.Create(command.UserId, command.Name, command.Icon);

        await profileRepository.AddAsync(profile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new Result(profile.Id);
    }
}
