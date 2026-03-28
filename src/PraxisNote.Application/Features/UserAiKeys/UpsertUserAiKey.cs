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

    private static readonly HashSet<string> KnownModels = new(StringComparer.OrdinalIgnoreCase)
    {
        // Anthropic
        "claude-sonnet-4-6",
        "claude-haiku-4-5",
        "claude-opus-4-6",
        // OpenAI
        "gpt-4o-mini",
        "gpt-4o",
        "gpt-4.1",
        // Gemini
        "gemini-1.5-flash",
        "gemini-1.5-pro",
        "gemini-2.0-flash",
    };

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(command.PreferredModel) && !KnownModels.Contains(command.PreferredModel))
        {
            throw new ArgumentException($"Unknown model: {command.PreferredModel}");
        }

        // Model-only update: no API key provided
        if (string.IsNullOrWhiteSpace(command.ApiKey))
        {
            var existing = await repository.GetByUserAndProviderAsync(command.UserId, command.Provider, cancellationToken);
            if (existing is null)
            {
                throw new UserAiKeyNotFoundException(command.UserId, command.Provider);
            }

            existing.UpdateModel(command.PreferredModel);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var encrypted = encryption.Encrypt(command.ApiKey);
        var hint = encryption.ComputeHint(command.ApiKey);

        await repository.UpsertAsync(command.UserId, command.Provider, encrypted, hint, command.PreferredModel, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
