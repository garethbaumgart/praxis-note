using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Profiles;
using PraxisNote.Domain.Aggregates.Users;

namespace PraxisNote.Application.Features.AccountLinking;

public enum MergeStrategy
{
    MergeIntoExisting,
    CreateNewProfile,
    Cancel
}

public sealed class RedeemLinkCode(
    IAccountLinkCodeRepository accountLinkCodeRepository,
    ILinkedIdentityRepository linkedIdentityRepository,
    IUserRepository userRepository,
    IProfileRepository profileRepository,
    IUnitOfWork unitOfWork)
{
    public const string InvalidCodeError = "Invalid or expired link code";
    public const string AlreadyLinkedError = "This account is already linked to a user";
    public const string AlreadyLinkedToTargetError = "These accounts are already linked";
    public const string SameUserError = "Cannot link an account to itself";
    public const string CancelledError = "Link operation cancelled by user";

    public record Command(
        Guid RedeemingUserId,
        string Code,
        MergeStrategy Strategy,
        Guid? TargetProfileId = null);

    public record Result(Guid TargetUserId, bool Success, string? Error = null);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        if (command.Strategy == MergeStrategy.Cancel)
        {
            return new Result(command.RedeemingUserId, false, CancelledError);
        }

        // Hash the provided code and do a targeted DB lookup
        var codeHash = LinkCodeService.HashCode(command.Code);

        var matchingCode = await accountLinkCodeRepository.GetByHashAsync(codeHash, cancellationToken);

        if (matchingCode is null || !matchingCode.IsValid())
        {
            return new Result(command.RedeemingUserId, false, InvalidCodeError);
        }

        var codeOwnerUserId = matchingCode.UserId;

        // Cannot link to yourself
        if (codeOwnerUserId == command.RedeemingUserId)
        {
            return new Result(command.RedeemingUserId, false, SameUserError);
        }

        // Verify the code owner still exists (they could have been deleted since code generation)
        var codeOwner = await userRepository.GetByIdAsync(codeOwnerUserId, cancellationToken);
        if (codeOwner is null)
        {
            return new Result(command.RedeemingUserId, false, InvalidCodeError);
        }

        // Check if the redeeming user's identity is already linked elsewhere
        var redeemingUser = await userRepository.GetByIdAsync(command.RedeemingUserId, cancellationToken);
        if (redeemingUser is null)
        {
            return new Result(command.RedeemingUserId, false, "Redeeming user not found");
        }

        var existingLink = await linkedIdentityRepository.GetByProviderAsync(
            redeemingUser.ExternalIdentity.Provider,
            redeemingUser.ExternalIdentity.ProviderId,
            cancellationToken);

        if (existingLink is not null)
        {
            if (existingLink.UserId == codeOwnerUserId)
            {
                // Already linked to the target account — nothing to do
                return new Result(codeOwnerUserId, false, AlreadyLinkedToTargetError);
            }

            if (existingLink.UserId != redeemingUser.Id)
            {
                // Linked to a different account — block
                return new Result(command.RedeemingUserId, false, AlreadyLinkedError);
            }

            // Self-link (seeded by migration): remove so the new LinkedIdentity
            // on the code owner's account can be created without violating the
            // unique constraint on (Provider, ProviderId)
            linkedIdentityRepository.Remove(existingLink);
        }

        // Mark code as redeemed
        matchingCode.MarkRedeemed();

        // Determine target profile based on strategy
        Guid targetProfileId;

        switch (command.Strategy)
        {
            case MergeStrategy.MergeIntoExisting:
            {
                if (command.TargetProfileId.HasValue)
                {
                    // Verify the target profile belongs to the code owner
                    var targetProfile = await profileRepository.GetByIdAsync(
                        command.TargetProfileId.Value, cancellationToken);

                    if (targetProfile is null || targetProfile.UserId != codeOwnerUserId)
                    {
                        return new Result(command.RedeemingUserId, false, "Target profile not found");
                    }

                    targetProfileId = targetProfile.Id;
                }
                else
                {
                    // Use the code owner's default profile
                    var defaultProfile = await profileRepository.GetDefaultByUserIdAsync(
                        codeOwnerUserId, cancellationToken);

                    if (defaultProfile is null)
                    {
                        return new Result(command.RedeemingUserId, false, "Code owner has no default profile");
                    }

                    targetProfileId = defaultProfile.Id;
                }

                break;
            }

            case MergeStrategy.CreateNewProfile:
            {
                // Create a new profile on the code owner's account
                var profileCount = await profileRepository.GetCountByUserIdAsync(
                    codeOwnerUserId, cancellationToken);

                if (profileCount >= 5)
                {
                    return new Result(command.RedeemingUserId, false, "Maximum number of profiles reached");
                }

                var email = redeemingUser.Email.Value;
                var profileName = email.Length <= 100 ? email : email[..97] + "...";
                var newProfile = Profile.Create(codeOwnerUserId, profileName);
                await profileRepository.AddAsync(newProfile, cancellationToken);

                targetProfileId = newProfile.Id;
                break;
            }

            default:
                return new Result(command.RedeemingUserId, false, "Invalid merge strategy");
        }

        // Create a LinkedIdentity on the code owner from the redeeming user's ExternalIdentity
        var linkedIdentity = LinkedIdentity.Create(
            userId: codeOwnerUserId,
            provider: redeemingUser.ExternalIdentity.Provider,
            providerId: redeemingUser.ExternalIdentity.ProviderId,
            email: redeemingUser.Email.Value,
            name: redeemingUser.Name,
            avatarUrl: redeemingUser.AvatarUrl,
            defaultProfileId: targetProfileId);

        await linkedIdentityRepository.AddAsync(linkedIdentity, cancellationToken);

        // Delete User B (the redeeming user) so their ExternalIdentity no longer
        // matches in Step 1 of login. Without this, logging in with the linked
        // Google account would still resolve to the old User B instead of
        // reaching Step 2 (LinkedIdentity lookup) which resolves to User A.
        // Cascade delete will clean up User B's profiles and other owned data.
        userRepository.Remove(redeemingUser);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new Result(codeOwnerUserId, true);
    }
}
