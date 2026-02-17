using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.ApiKeys;

namespace PraxisNote.Application.Features.ApiKeys;

public sealed class CreateApiKey(IApiKeyRepository apiKeyRepository, IUnitOfWork unitOfWork)
{
    public const int MaxKeysPerUser = 5;
    public const string TooManyKeysError = "API_KEY_LIMIT_REACHED";

    public record Command(Guid UserId, Guid ProfileId, string Name, DateTimeOffset? ExpiresAt = null);
    public record Result(Guid ApiKeyId, string RawKey, string Prefix);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var existing = await apiKeyRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        var activeCount = existing.Count(k => k.IsValid);
        if (activeCount >= MaxKeysPerUser)
            throw new InvalidOperationException(TooManyKeysError);

        var (apiKey, rawKey) = ApiKey.Create(command.UserId, command.ProfileId, command.Name, command.ExpiresAt);
        await apiKeyRepository.AddAsync(apiKey, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new Result(apiKey.Id, rawKey, apiKey.KeyPrefix);
    }
}
