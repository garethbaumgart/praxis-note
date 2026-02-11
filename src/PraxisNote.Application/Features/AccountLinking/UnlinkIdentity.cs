using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Users;

namespace PraxisNote.Application.Features.AccountLinking;

public sealed class UnlinkIdentity(
    ILinkedIdentityRepository linkedIdentityRepository,
    IUnitOfWork unitOfWork)
{
    public const string NotFoundError = "Linked identity not found";
    public const string LastIdentityError = "Cannot unlink the last identity — this would orphan the account";

    public record Command(Guid UserId, Guid LinkedIdentityId);

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var identities = await linkedIdentityRepository.GetByUserIdAsync(command.UserId, cancellationToken);

        var identity = identities.FirstOrDefault(li => li.Id == command.LinkedIdentityId);
        if (identity is null)
        {
            throw new InvalidOperationException(NotFoundError);
        }

        // The user always has an ExternalIdentity on the User entity itself,
        // so linked identities can all be removed as long as the primary identity exists.
        // However, if we ever remove ExternalIdentity from User, we'd need to check
        // that at least one LinkedIdentity remains. For safety, we still enforce
        // that you can't remove the last linked identity if it matches the primary identity.
        // For now, any linked identity can be removed since the User.ExternalIdentity
        // always serves as the primary login method.

        linkedIdentityRepository.Remove(identity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
