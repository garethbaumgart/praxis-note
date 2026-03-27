using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PraxisNote.Application.Features.Tags.Services;

namespace PraxisNote.Infrastructure.External;

public sealed class GeminiTagAiChatService(
    HttpClient httpClient,
    ILogger<GeminiTagAiChatService> logger,
    string apiKey,
    string model,
    int maxTokens = 4096,
    int timeoutSeconds = 120) : ITagAiChatService
{
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    public async IAsyncEnumerable<string> StreamResponseAsync(
        TagChatContext context,
        string userMessage,
        IReadOnlyList<ChatMessage> history,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var contextBlock = AnthropicTagAiChatService.BuildContextBlock(context);
        var systemPrompt = AnthropicTagAiChatService.SystemPromptTemplate
            .Replace("{0}", context.TagName)
            .Replace("{1}", contextBlock);

        var contents = new List<GeminiMeetingAnalyzer.GeminiContent>();

        // Add conversation history
        foreach (var msg in history)
        {
            contents.Add(new GeminiMeetingAnalyzer.GeminiContent
            {
                Role = msg.Role == "user" ? "user" : "model",
                Parts = [new GeminiMeetingAnalyzer.GeminiPart { Text = msg.Content }]
            });
        }

        // Add the current user message
        contents.Add(new GeminiMeetingAnalyzer.GeminiContent
        {
            Role = "user",
            Parts = [new GeminiMeetingAnalyzer.GeminiPart { Text = userMessage }]
        });

        var requestBody = new GeminiStreamRequest
        {
            SystemInstruction = new GeminiMeetingAnalyzer.GeminiContent
            {
                Parts = [new GeminiMeetingAnalyzer.GeminiPart { Text = systemPrompt }]
            },
            Contents = contents,
            GenerationConfig = new GeminiMeetingAnalyzer.GeminiGenerationConfig { MaxOutputTokens = maxTokens }
        };

        var url = $"{BaseUrl}/{model}:streamGenerateContent?alt=sse&key={apiKey}";

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        logger.LogDebug("Starting tag AI chat stream with Gemini model {Model}", model);

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(requestBody, options: GeminiJsonOptions)
        };

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
        using var reader = new System.IO.StreamReader(stream);

        while (await reader.ReadLineAsync(cts.Token) is { } line)
        {

            if (!line.StartsWith("data: ", StringComparison.Ordinal))
                continue;

            var json = line[6..];
            if (string.IsNullOrWhiteSpace(json))
                continue;

            var chunk = JsonSerializer.Deserialize<GeminiMeetingAnalyzer.GeminiResponse>(json, GeminiJsonOptions);
            var text = chunk?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (!string.IsNullOrEmpty(text))
            {
                yield return text;
            }
        }
    }

    public async Task<IReadOnlyList<string>> GenerateStarterPromptsAsync(
        TagChatContext context,
        CancellationToken cancellationToken = default)
    {
        var contextBlock = AnthropicTagAiChatService.BuildContextBlock(context);
        var prompt = AnthropicTagAiChatService.StarterPrompt
            .Replace("{0}", context.TagName)
            .Replace("{1}", contextBlock);

        var url = $"{BaseUrl}/{model}:generateContent?key={apiKey}";

        var requestBody = new GeminiMeetingAnalyzer.GeminiRequest
        {
            Contents = [new GeminiMeetingAnalyzer.GeminiContent
            {
                Parts = [new GeminiMeetingAnalyzer.GeminiPart { Text = prompt }]
            }],
            GenerationConfig = new GeminiMeetingAnalyzer.GeminiGenerationConfig { MaxOutputTokens = 512 }
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        logger.LogDebug("Generating starter prompts with Gemini model {Model}", model);

        var response = await httpClient.PostAsJsonAsync(url, requestBody, GeminiJsonOptions, cts.Token);
        response.EnsureSuccessStatusCode();

        var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiMeetingAnalyzer.GeminiResponse>(GeminiJsonOptions, cts.Token);
        var content = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(content))
        {
            return AnthropicTagAiChatService.DefaultStarters(context.TagName);
        }

        try
        {
            var cleanJson = AnthropicMeetingAnalyzer.CleanJsonResponse(content);
            var starters = JsonSerializer.Deserialize<List<string>>(cleanJson);
            if (starters is { Count: > 0 })
            {
                return starters.Take(4).ToList();
            }
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse Gemini starter prompts JSON, using defaults");
        }

        return AnthropicTagAiChatService.DefaultStarters(context.TagName);
    }

    private static readonly JsonSerializerOptions GeminiJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    internal sealed class GeminiStreamRequest
    {
        public GeminiMeetingAnalyzer.GeminiContent? SystemInstruction { get; set; }
        public List<GeminiMeetingAnalyzer.GeminiContent> Contents { get; set; } = [];
        public GeminiMeetingAnalyzer.GeminiGenerationConfig? GenerationConfig { get; set; }
    }
}
