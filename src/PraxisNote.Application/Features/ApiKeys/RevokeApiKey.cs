using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.ApiKeys;

namespace PraxisNote.Application.Features.ApiKeys;

public sealed class RevokeApiKey(IApiKeyRepository apiKeyRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid ApiKeyId);

    public async Task<bool> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var keys = await apiKeyRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        var key = keys.FirstOrDefault(k => k.Id == command.ApiKeyId);
        if (key is null) return false;
        key.Revoke();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
