namespace PraxisNote.Web.Endpoints;

internal static class AiKeyErrorResults
{
    internal static IResult NoAiKeyResult() => Results.UnprocessableEntity(new
    {
        error = "no_ai_key",
        message = "No AI API key configured. Add your key in Settings to use this feature.",
        settingsUrl = "/settings/ai-keys"
    });
}
