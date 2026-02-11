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

        // Prevent removing the last linked identity — every user must retain at least one
        // so that login can resolve to this account via the LinkedIdentity table.
        if (identities.Count <= 1)
        {
            throw new InvalidOperationException(LastIdentityError);
        }

        linkedIdentityRepository.Remove(identity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
