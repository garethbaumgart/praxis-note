using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.UserAiKeys;

namespace PraxisNote.Application.Features.UserAiKeys;

public sealed class DeleteUserAiKey(IUserAiKeyRepository repository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, AiProvider Provider);

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetByUserAndProviderAsync(command.UserId, command.Provider, cancellationToken);
        if (existing is null)
            throw new UserAiKeyNotFoundException(command.UserId, command.Provider);

        repository.Remove(existing);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
