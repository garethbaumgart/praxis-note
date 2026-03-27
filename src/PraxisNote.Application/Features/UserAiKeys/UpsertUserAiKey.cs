using PraxisNote.Application.Common;
using PraxisNote.Application.Features.UserAiKeys.Services;
using PraxisNote.Domain.Aggregates.UserAiKeys;

namespace PraxisNote.Application.Features.UserAiKeys;

public sealed class UpsertUserAiKey(
    IUserAiKeyRepository repository,
    IAiKeyEncryptionService encryption,
    IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, AiProvider Provider, string ApiKey, string? PreferredModel);

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ApiKey, nameof(command.ApiKey));

        var encrypted = encryption.Encrypt(command.ApiKey);
        var hint = encryption.ComputeHint(command.ApiKey);

        await repository.UpsertAsync(command.UserId, command.Provider, encrypted, hint, command.PreferredModel, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
