using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Profiles;

namespace PraxisNote.Application.Features.Profiles;

public sealed class SetDefaultProfile(
    IProfileRepository profileRepository,
    IUnitOfWork unitOfWork)
{
    public const string NotFoundError = "Profile not found";

    public record Command(Guid UserId, Guid ProfileId);

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var profiles = await profileRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        var target = profiles.FirstOrDefault(p => p.Id == command.ProfileId);

        if (target is null)
        {
            throw new InvalidOperationException(NotFoundError);
        }

        // Clear default from all profiles
        foreach (var profile in profiles)
        {
            if (profile.IsDefault)
            {
                profile.ClearDefault();
            }
        }

        // Set new default
        target.SetAsDefault();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
