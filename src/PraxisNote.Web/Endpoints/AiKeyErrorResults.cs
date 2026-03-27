using System.Text.Json;

namespace PraxisNote.Web.Endpoints;

internal static class AiKeyErrorResults
{
    private const string ErrorCode = "no_ai_key";
    private const string Message = "No AI API key configured. Add your key in Settings to use this feature.";
    private const string SettingsUrl = "/settings/ai-keys";

    internal static IResult NoAiKeyResult() => Results.UnprocessableEntity(new
    {
        error = ErrorCode,
        message = Message,
        settingsUrl = SettingsUrl
    });

    internal static string NoAiKeySsePayload() => JsonSerializer.Serialize(new
    {
        error = ErrorCode,
        message = Message,
        settingsUrl = SettingsUrl
    });
}
