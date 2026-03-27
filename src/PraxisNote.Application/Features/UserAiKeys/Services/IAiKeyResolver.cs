using PraxisNote.Domain.Aggregates.UserAiKeys;

namespace PraxisNote.Application.Features.UserAiKeys.Services;

public record ResolvedAiKey(AiProvider Provider, string ApiKey, string Model);

public interface IAiKeyResolver
{
    Task<ResolvedAiKey?> ResolveAsync(Guid userId, CancellationToken ct = default);
}

public sealed class NoAiKeyConfiguredException() : Exception(
    "No AI key is configured. Add your own API key in Settings → AI Keys, or ask your administrator to configure a default key.");
