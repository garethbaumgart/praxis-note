using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Application.Features.Tags.Services;
using PraxisNote.Domain.Aggregates.UserAiKeys;

namespace PraxisNote.Infrastructure.External;

public sealed class AiProviderFactory(
    IOptions<AiProviderSettings> settings,
    IHttpClientFactory httpClientFactory,
    ILoggerFactory loggerFactory) : IAiProviderFactory
{
    private readonly AiProviderSettings _settings = settings.Value;

    public IMeetingAnalyzer CreateMeetingAnalyzer(string apiKey, AiProvider provider, string model)
    {
        return provider switch
        {
            AiProvider.Anthropic => new AnthropicMeetingAnalyzer(
                Microsoft.Extensions.Options.Options.Create(
                    CreateSettingsWithKey(apiKey, provider, model)),
                loggerFactory.CreateLogger<AnthropicMeetingAnalyzer>()),

            AiProvider.Gemini => new GeminiMeetingAnalyzer(
                httpClientFactory.CreateClient($"Gemini-{Guid.NewGuid()}"),
                loggerFactory.CreateLogger<GeminiMeetingAnalyzer>(),
                apiKey,
                model,
                _settings.MaxTokens,
                _settings.TimeoutSeconds),

            AiProvider.OpenAI => new OpenAiMeetingAnalyzer(
                loggerFactory.CreateLogger<OpenAiMeetingAnalyzer>(),
                apiKey,
                model,
                _settings.MaxTokens,
                _settings.TimeoutSeconds),

            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported AI provider")
        };
    }

    public ITagAiChatService CreateTagAiChatService(string apiKey, AiProvider provider, string model)
    {
        return provider switch
        {
            AiProvider.Anthropic => new AnthropicTagAiChatService(
                Microsoft.Extensions.Options.Options.Create(
                    CreateSettingsWithKey(apiKey, provider, model)),
                loggerFactory.CreateLogger<AnthropicTagAiChatService>()),

            AiProvider.Gemini => new GeminiTagAiChatService(
                httpClientFactory.CreateClient($"Gemini-{Guid.NewGuid()}"),
                loggerFactory.CreateLogger<GeminiTagAiChatService>(),
                apiKey,
                model,
                _settings.MaxTokens,
                _settings.TimeoutSeconds),

            AiProvider.OpenAI => new OpenAiTagAiChatService(
                loggerFactory.CreateLogger<OpenAiTagAiChatService>(),
                apiKey,
                model,
                _settings.MaxTokens,
                _settings.TimeoutSeconds),

            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported AI provider")
        };
    }

    private AiProviderSettings CreateSettingsWithKey(string apiKey, AiProvider provider, string model)
    {
        var settings = new AiProviderSettings
        {
            MaxTokens = _settings.MaxTokens,
            TimeoutSeconds = _settings.TimeoutSeconds,
            Anthropic = new AnthropicProviderConfig
            {
                ApiKey = provider == AiProvider.Anthropic ? apiKey : _settings.Anthropic.ApiKey,
                DefaultModel = provider == AiProvider.Anthropic ? model : _settings.Anthropic.DefaultModel
            },
            OpenAI = new OpenAiProviderConfig
            {
                ApiKey = provider == AiProvider.OpenAI ? apiKey : _settings.OpenAI.ApiKey,
                DefaultModel = provider == AiProvider.OpenAI ? model : _settings.OpenAI.DefaultModel
            },
            Gemini = new GeminiProviderConfig
            {
                ApiKey = provider == AiProvider.Gemini ? apiKey : _settings.Gemini.ApiKey,
                DefaultModel = provider == AiProvider.Gemini ? model : _settings.Gemini.DefaultModel
            }
        };

        return settings;
    }
}
