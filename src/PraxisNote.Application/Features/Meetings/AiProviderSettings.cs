namespace PraxisNote.Application.Features.Meetings;

public class AiProviderSettings
{
    public const string SectionName = "AiProviders";

    public int MaxTokens { get; set; } = 4096;
    public int TimeoutSeconds { get; set; } = 120;

    public AnthropicProviderConfig Anthropic { get; set; } = new();
    public OpenAiProviderConfig OpenAI { get; set; } = new();
    public GeminiProviderConfig Gemini { get; set; } = new();
}

public class AnthropicProviderConfig
{
    public string? ApiKey { get; set; }
    public string DefaultModel { get; set; } = "claude-sonnet-4-6";
}

public class OpenAiProviderConfig
{
    public string? ApiKey { get; set; }
    public string DefaultModel { get; set; } = "gpt-4o-mini";
}

public class GeminiProviderConfig
{
    public string? ApiKey { get; set; }
    public string DefaultModel { get; set; } = "gemini-2.0-flash";
}
