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

    // Ordered lists — first entry is the recommended/default model shown in the UI
    private static readonly Dictionary<AiProvider, List<string>> KnownModelsByProvider = new()
    {
        [AiProvider.Anthropic] =
        [
            "claude-sonnet-4-6",
            "claude-haiku-4-5",
            "claude-opus-4-6",
        ],
        [AiProvider.OpenAI] =
        [
            "gpt-4o-mini",
            "gpt-4o",
            "gpt-4.1",
        ],
        [AiProvider.Gemini] =
        [
            "gemini-2.0-flash",   // default — available on free tier v1beta
            "gemini-1.5-flash",   // kept for backwards compat with saved preferences
            "gemini-1.5-pro",
        ],
    };

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        // Normalize model to canonical casing from the allowlist
        var normalizedModel = NormalizeModel(command.Provider, command.PreferredModel);

        // Model-only update: no API key provided
        if (string.IsNullOrWhiteSpace(command.ApiKey))
        {
            if (string.IsNullOrWhiteSpace(normalizedModel))
                throw new ArgumentException("PreferredModel is required for model-only updates");

            var existing = await repository.GetByUserAndProviderAsync(command.UserId, command.Provider, cancellationToken);
            if (existing is null)
            {
                throw new UserAiKeyNotFoundException(command.UserId, command.Provider);
            }

            existing.UpdateModel(normalizedModel);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var encrypted = encryption.Encrypt(command.ApiKey);
        var hint = encryption.ComputeHint(command.ApiKey);

        // Preserve existing model preference when no explicit model is provided
        if (normalizedModel is null)
        {
            var existing = await repository.GetByUserAndProviderAsync(command.UserId, command.Provider, cancellationToken);
            if (existing is not null)
                normalizedModel = existing.PreferredModel;
        }

        await repository.UpsertAsync(command.UserId, command.Provider, encrypted, hint, normalizedModel, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string? NormalizeModel(AiProvider provider, string? preferredModel)
    {
        if (string.IsNullOrWhiteSpace(preferredModel))
            return null;

        if (!KnownModelsByProvider.TryGetValue(provider, out var allowed))
            throw new ArgumentException($"No model configuration for provider: {provider}");

        // Find the canonical casing from the allowlist
        var canonical = allowed.FirstOrDefault(m => string.Equals(m, preferredModel, StringComparison.OrdinalIgnoreCase));
        if (canonical is null)
            throw new ArgumentException($"Unknown model: {preferredModel}");

        return canonical;
    }
}
