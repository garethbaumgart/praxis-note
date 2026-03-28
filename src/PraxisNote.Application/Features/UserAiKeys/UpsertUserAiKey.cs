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

    private static readonly Dictionary<AiProvider, HashSet<string>> KnownModelsByProvider = new()
    {
        [AiProvider.Anthropic] = new(StringComparer.OrdinalIgnoreCase)
        {
            "claude-sonnet-4-6",
            "claude-haiku-4-5",
            "claude-opus-4-6",
        },
        [AiProvider.OpenAI] = new(StringComparer.OrdinalIgnoreCase)
        {
            "gpt-4o-mini",
            "gpt-4o",
            "gpt-4.1",
        },
        [AiProvider.Gemini] = new(StringComparer.OrdinalIgnoreCase)
        {
            "gemini-1.5-flash",
            "gemini-1.5-pro",
            "gemini-2.0-flash",
        },
    };

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        // Normalize model to canonical casing from the allowlist
        var normalizedModel = NormalizeModel(command.Provider, command.PreferredModel);

        // Model-only update: no API key provided
        if (string.IsNullOrWhiteSpace(command.ApiKey))
        {
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

        await repository.UpsertAsync(command.UserId, command.Provider, encrypted, hint, normalizedModel, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string? NormalizeModel(AiProvider provider, string? preferredModel)
    {
        if (string.IsNullOrWhiteSpace(preferredModel))
            return null;

        if (!KnownModelsByProvider.TryGetValue(provider, out var allowed))
            throw new ArgumentException($"Unknown model: {preferredModel}");

        // Find the canonical casing from the allowlist
        var canonical = allowed.FirstOrDefault(m => string.Equals(m, preferredModel, StringComparison.OrdinalIgnoreCase));
        if (canonical is null)
            throw new ArgumentException($"Unknown model: {preferredModel}");

        return canonical;
    }
}
