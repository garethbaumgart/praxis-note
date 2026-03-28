using System.Text.Json;
using PraxisNote.Application.Features.UserAiKeys;

namespace PraxisNote.Web.Endpoints;

internal static class AiKeyErrorResults
{
    private const string SettingsUrl = "/settings/ai-keys";

    internal static IResult NoAiKeyResult() => Results.UnprocessableEntity(new
    {
        error = "no_ai_key",
        message = "No AI API key configured. Add your key in Settings to use this feature.",
        settingsUrl = SettingsUrl
    });

    internal static IResult AiKeyInvalidResult(AiKeyInvalidException ex) => Results.UnprocessableEntity(new
    {
        error = "ai_key_invalid",
        message = $"Your API key was rejected by {ex.Provider}. Please update it in Settings.",
        settingsUrl = SettingsUrl
    });

    internal static IResult AiRateLimitedResult(AiRateLimitedException ex) => Results.UnprocessableEntity(new
    {
        error = "ai_rate_limited",
        message = $"Rate limit reached with {ex.Provider}. Wait a moment and try again.",
        retryAfterSeconds = ex.RetryAfterSeconds
    });

    internal static IResult AiProviderErrorResult(AiProviderException ex) => Results.UnprocessableEntity(new
    {
        error = "ai_provider_error",
        message = ex.Message
    });

    internal static string NoAiKeySsePayload() => JsonSerializer.Serialize(new
    {
        error = "no_ai_key",
        message = "No AI API key configured. Add your key in Settings to use this feature.",
        settingsUrl = SettingsUrl
    });

    internal static string AiKeyInvalidSsePayload(AiKeyInvalidException ex) => JsonSerializer.Serialize(new
    {
        error = "ai_key_invalid",
        message = $"Your API key was rejected by {ex.Provider}. Please update it in Settings.",
        settingsUrl = SettingsUrl
    });

    internal static string AiRateLimitedSsePayload(AiRateLimitedException ex) => JsonSerializer.Serialize(new
    {
        error = "ai_rate_limited",
        message = $"Rate limit reached with {ex.Provider}. Wait a moment and try again.",
        retryAfterSeconds = ex.RetryAfterSeconds
    });

    internal static string AiProviderErrorSsePayload(AiProviderException ex) => JsonSerializer.Serialize(new
    {
        error = "ai_provider_error",
        message = ex.Message
    });
}
