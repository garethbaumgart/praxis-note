using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Profiles;
using PraxisNote.Domain.Aggregates.Users;
using PraxisNote.Domain.ValueObjects;

namespace PraxisNote.Application.Features.Users;

public record LoginOrRegisterCommand(
    string Provider,
    string ProviderId,
    string Email,
    string Name,
    string? AvatarUrl);

public record LoginOrRegisterResult(Guid UserId, bool IsNewUser);

public sealed class LoginOrRegisterUser(
    IUserRepository userRepository,
    ILinkedIdentityRepository linkedIdentityRepository,
    IProfileRepository profileRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<LoginOrRegisterResult> ExecuteAsync(
        LoginOrRegisterCommand command,
        CancellationToken cancellationToken = default)
    {
        var externalIdentity = new ExternalIdentity(command.Provider, command.ProviderId);

        // Step 1: Look up by ExternalIdentity (existing flow)
        var existingUser = await userRepository.GetByExternalIdentityAsync(
            externalIdentity, cancellationToken);

        if (existingUser is not null)
        {
            existingUser.RecordLogin(command.AvatarUrl);
            userRepository.Update(existingUser);

            // Ensure user has at least one profile (handles migration of existing users)
            await EnsureDefaultProfileAsync(existingUser.Id, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new LoginOrRegisterResult(existingUser.Id, IsNewUser: false);
        }

        // Step 2: Check LinkedIdentity table for a linked account
        var linkedIdentity = await linkedIdentityRepository.GetByProviderAsync(
            externalIdentity.Provider, externalIdentity.ProviderId, cancellationToken);

        if (linkedIdentity is not null)
        {
            var linkedUser = await userRepository.GetByIdAsync(linkedIdentity.UserId, cancellationToken);
            if (linkedUser is not null)
            {
                linkedUser.RecordLogin(command.AvatarUrl);
                userRepository.Update(linkedUser);

                await EnsureDefaultProfileAsync(linkedUser.Id, cancellationToken);

                await unitOfWork.SaveChangesAsync(cancellationToken);
                return new LoginOrRegisterResult(linkedUser.Id, IsNewUser: false);
            }

            // Orphaned linked identity — the referenced user no longer exists.
            // Remove the stale record so it doesn't block future linking.
            linkedIdentityRepository.Remove(linkedIdentity);
        }

        // Step 3: No match found — create new user + default profile
        var email = new Email(command.Email);
        var newUser = User.Register(externalIdentity, email, command.Name, command.AvatarUrl);

        await userRepository.AddAsync(newUser, cancellationToken);

        // Create default profile for new user
        var defaultProfile = Profile.CreateDefault(newUser.Id);
        await profileRepository.AddAsync(defaultProfile, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginOrRegisterResult(newUser.Id, IsNewUser: true);
    }

    /// <summary>
    /// Ensures the user has at least one profile. Creates a default profile if none exist.
    /// This handles the case where existing users don't yet have profiles after migration.
    /// </summary>
    private async Task EnsureDefaultProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var defaultProfile = await profileRepository.GetDefaultByUserIdAsync(userId, cancellationToken);
        if (defaultProfile is null)
        {
            var newDefault = Profile.CreateDefault(userId);
            await profileRepository.AddAsync(newDefault, cancellationToken);
        }
    }
}
