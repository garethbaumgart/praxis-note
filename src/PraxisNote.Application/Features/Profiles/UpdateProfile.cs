using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Profiles;

namespace PraxisNote.Application.Features.Profiles;

public sealed class UpdateProfile(
    IProfileRepository profileRepository,
    IUnitOfWork unitOfWork)
{
    public const string NotFoundError = "Profile not found";

    public record Command(Guid UserId, Guid ProfileId, string Name, string? Icon);

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var profile = await profileRepository.GetByIdAsync(command.ProfileId, cancellationToken);
        if (profile is null || profile.UserId != command.UserId)
        {
            throw new InvalidOperationException(NotFoundError);
        }

        profile.Rename(command.Name);
        profile.SetIcon(command.Icon);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
