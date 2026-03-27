using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.UserAiKeys.Services;
using PraxisNote.Domain.Aggregates.UserAiKeys;

namespace PraxisNote.Infrastructure.External;

public sealed class AiKeyResolver(
    IUserAiKeyRepository userAiKeyRepository,
    IAiKeyEncryptionService encryption,
    IOptions<AiProviderSettings> settings,
    ILogger<AiKeyResolver> logger) : IAiKeyResolver
{
    private static readonly AiProvider[] UserKeyPriority = [AiProvider.Anthropic, AiProvider.OpenAI, AiProvider.Gemini];

    private readonly AiProviderSettings _settings = settings.Value;

    public async Task<ResolvedAiKey?> ResolveAsync(Guid userId, CancellationToken ct = default)
    {
        // 1. User key — prefer Anthropic → OpenAI → Gemini
        var userKeys = await userAiKeyRepository.GetByUserIdAsync(userId, ct);
        if (userKeys.Count > 0)
        {
            foreach (var provider in UserKeyPriority)
            {
                var key = userKeys.FirstOrDefault(k => k.Provider == provider);
                if (key is not null)
                {
                    try
                    {
                        var decrypted = encryption.Decrypt(key.EncryptedKey);
                        var model = key.PreferredModel ?? GetDefaultModel(provider);
                        logger.LogDebug("Resolved user key for provider {Provider} for user {UserId}", provider, userId);
                        return new ResolvedAiKey(provider, decrypted, model);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to decrypt user key for provider {Provider} for user {UserId}, trying next provider", provider, userId);
                    }
                }
            }
        }

        // 2. App default Anthropic key
        if (!string.IsNullOrWhiteSpace(_settings.Anthropic.ApiKey))
        {
            logger.LogDebug("Resolved app default Anthropic key for user {UserId}", userId);
            return new ResolvedAiKey(AiProvider.Anthropic, _settings.Anthropic.ApiKey, _settings.Anthropic.DefaultModel);
        }

        // 3. App default OpenAI key
        if (!string.IsNullOrWhiteSpace(_settings.OpenAI.ApiKey))
        {
            logger.LogDebug("Resolved app default OpenAI key for user {UserId}", userId);
            return new ResolvedAiKey(AiProvider.OpenAI, _settings.OpenAI.ApiKey, _settings.OpenAI.DefaultModel);
        }

        // 4. Gemini free tier key
        if (!string.IsNullOrWhiteSpace(_settings.Gemini.ApiKey))
        {
            logger.LogDebug("Resolved Gemini free tier key for user {UserId}", userId);
            return new ResolvedAiKey(AiProvider.Gemini, _settings.Gemini.ApiKey, _settings.Gemini.DefaultModel);
        }

        // 5. No key available
        logger.LogWarning("No AI key could be resolved for user {UserId}", userId);
        return null;
    }

    private string GetDefaultModel(AiProvider provider) => provider switch
    {
        AiProvider.Anthropic => _settings.Anthropic.DefaultModel,
        AiProvider.OpenAI => _settings.OpenAI.DefaultModel,
        AiProvider.Gemini => _settings.Gemini.DefaultModel,
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
    };
}
