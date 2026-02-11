using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Profiles;
using PraxisNote.Domain.Aggregates.Users;

namespace PraxisNote.Application.Features.AccountLinking;

public sealed class SetIdentityDefaultProfile(
    ILinkedIdentityRepository linkedIdentityRepository,
    IProfileRepository profileRepository,
    IUnitOfWork unitOfWork)
{
    public const string IdentityNotFoundError = "Linked identity not found";
    public const string ProfileNotFoundError = "Profile not found";

    public record Command(Guid UserId, Guid LinkedIdentityId, Guid? ProfileId);

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var identities = await linkedIdentityRepository.GetByUserIdAsync(command.UserId, cancellationToken);

        var identity = identities.FirstOrDefault(li => li.Id == command.LinkedIdentityId);
        if (identity is null)
        {
            throw new InvalidOperationException(IdentityNotFoundError);
        }

        // If setting a profile, verify it belongs to this user
        if (command.ProfileId.HasValue)
        {
            var profile = await profileRepository.GetByIdAsync(command.ProfileId.Value, cancellationToken);
            if (profile is null || profile.UserId != command.UserId)
            {
                throw new InvalidOperationException(ProfileNotFoundError);
            }
        }

        identity.SetDefaultProfile(command.ProfileId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
