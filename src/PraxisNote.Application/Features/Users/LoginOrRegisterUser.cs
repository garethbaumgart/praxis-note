using PraxisNote.Application.Common;
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
    IUnitOfWork unitOfWork)
{
    public async Task<LoginOrRegisterResult> ExecuteAsync(
        LoginOrRegisterCommand command,
        CancellationToken cancellationToken = default)
    {
        var externalIdentity = new ExternalIdentity(command.Provider, command.ProviderId);

        var existingUser = await userRepository.GetByExternalIdentityAsync(
            externalIdentity, cancellationToken);

        if (existingUser is not null)
        {
            existingUser.RecordLogin(command.AvatarUrl);
            userRepository.Update(existingUser);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new LoginOrRegisterResult(existingUser.Id, IsNewUser: false);
        }

        var email = new Email(command.Email);
        var newUser = User.Register(externalIdentity, email, command.Name, command.AvatarUrl);

        await userRepository.AddAsync(newUser, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginOrRegisterResult(newUser.Id, IsNewUser: true);
    }
}
