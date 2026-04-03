using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PraxisNote.Application.Features.Tags.Services;
using PraxisNote.Application.Features.UserAiKeys;
using static PraxisNote.Infrastructure.External.GeminiJsonConfiguration;

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

        var contents = new List<GeminiContent>();

        // Add conversation history
        foreach (var msg in history)
        {
            contents.Add(new GeminiContent
            {
                Role = msg.Role == "user" ? "user" : "model",
                Parts = [new GeminiPart { Text = msg.Content }]
            });
        }

        // Add the current user message
        contents.Add(new GeminiContent
        {
            Role = "user",
            Parts = [new GeminiPart { Text = userMessage }]
        });

        var requestBody = new GeminiStreamRequest
        {
            SystemInstruction = new GeminiContent
            {
                Parts = [new GeminiPart { Text = systemPrompt }]
            },
            Contents = contents,
            GenerationConfig = new GeminiGenerationConfig { MaxOutputTokens = maxTokens }
        };

        var url = $"{BaseUrl}/{model}:streamGenerateContent?alt=sse";

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        logger.LogDebug("Starting tag AI chat stream with Gemini model {Model}", model);

        HttpResponseMessage response;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(requestBody, options: Options)
            };
            request.Headers.Add("x-goog-api-key", apiKey);

            response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var body = await response.Content.ReadAsStringAsync(cts.Token);
                if (body.Contains("quota", StringComparison.OrdinalIgnoreCase)
                    || body.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase))
                {
                    response.Dispose();
                    logger.LogWarning("Insufficient credits for {Provider}", "Gemini");
                    throw new AiInsufficientCreditsException("Gemini");
                }

                var retryAfterSeconds = response.Headers.RetryAfter?.Delta is { } delta
                    ? (int)Math.Ceiling(delta.TotalSeconds)
                    : response.Headers.RetryAfter?.Date is { } date
                        ? Math.Max(0, (int)Math.Ceiling((date - DateTimeOffset.UtcNow).TotalSeconds))
                        : (int?)null;
                response.Dispose();
                logger.LogWarning("Rate limited by {Provider}", "Gemini");
                throw new AiRateLimitedException("Gemini", retryAfterSeconds);
            }
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch
            {
                response.Dispose();
                throw;
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            logger.LogError(ex, "AI key rejected by {Provider}", "Gemini");
            throw new AiKeyInvalidException("Gemini");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Timeout calling {Provider}", "Gemini");
            throw new AiProviderException("Gemini", "Gemini is not responding. Try again shortly.", ex);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is { } s && (int)s >= 500)
        {
            logger.LogError(ex, "Provider error from {Provider}: {StatusCode}", "Gemini", ex.StatusCode);
            throw new AiProviderException("Gemini", "Gemini returned an error. Try again shortly.", ex);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error calling {Provider}", "Gemini");
            throw new AiProviderException("Gemini", "Could not reach Gemini. Check your connection and try again.", ex);
        }

        using (response)
        {
            System.IO.Stream stream;
            try
            {
                stream = await response.Content.ReadAsStreamAsync(cts.Token);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Timeout reading stream from {Provider}", "Gemini");
                throw new AiProviderException("Gemini", "Gemini is not responding. Try again shortly.", ex);
            }

            await using (stream)
            {
                using var reader = new System.IO.StreamReader(stream);

                while (true)
                {
                    string? line;
                    try
                    {
                        line = await reader.ReadLineAsync(cts.Token);
                    }
                    catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                    {
                        logger.LogError(ex, "Timeout reading stream from {Provider}", "Gemini");
                        throw new AiProviderException("Gemini", "Gemini is not responding. Try again shortly.", ex);
                    }

                    if (line is null) break;
                    if (!line.StartsWith("data: ", StringComparison.Ordinal)) continue;

                    var json = line[6..];
                    if (string.IsNullOrWhiteSpace(json)) continue;

                    var chunk = JsonSerializer.Deserialize<GeminiResponse>(json, Options);
                    var text = string.Concat(
                        chunk?.Candidates?
                            .SelectMany(c => c.Content?.Parts ?? [])
                            .Select(p => p.Text ?? string.Empty) ?? []);

                    if (!string.IsNullOrEmpty(text))
                    {
                        yield return text;
                    }
                }
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

        var url = $"{BaseUrl}/{model}:generateContent";

        var requestBody = new GeminiRequest
        {
            Contents = [new GeminiContent
            {
                Parts = [new GeminiPart { Text = prompt }]
            }],
            GenerationConfig = new GeminiGenerationConfig { MaxOutputTokens = 512 }
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        logger.LogDebug("Generating starter prompts with Gemini model {Model}", model);

        try
        {
            using var starterRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(requestBody, options: Options)
            };
            starterRequest.Headers.Add("x-goog-api-key", apiKey);

            using var response = await httpClient.SendAsync(starterRequest, cts.Token);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var body = await response.Content.ReadAsStringAsync(cts.Token);
                if (body.Contains("quota", StringComparison.OrdinalIgnoreCase)
                    || body.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning("Insufficient credits for {Provider}", "Gemini");
                    throw new AiInsufficientCreditsException("Gemini");
                }

                var retryAfterSeconds = response.Headers.RetryAfter?.Delta is { } delta
                    ? (int)Math.Ceiling(delta.TotalSeconds)
                    : response.Headers.RetryAfter?.Date is { } date
                        ? Math.Max(0, (int)Math.Ceiling((date - DateTimeOffset.UtcNow).TotalSeconds))
                        : (int?)null;
                logger.LogWarning("Rate limited by {Provider}", "Gemini");
                throw new AiRateLimitedException("Gemini", retryAfterSeconds);
            }
            response.EnsureSuccessStatusCode();

            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(Options, cts.Token);
            var content = string.Concat(
                geminiResponse?.Candidates?
                    .SelectMany(c => c.Content?.Parts ?? [])
                    .Select(p => p.Text ?? string.Empty) ?? []);

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
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            logger.LogError(ex, "AI key rejected by {Provider}", "Gemini");
            throw new AiKeyInvalidException("Gemini");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Timeout calling {Provider}", "Gemini");
            throw new AiProviderException("Gemini", "Gemini is not responding. Try again shortly.", ex);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is { } s && (int)s >= 500)
        {
            logger.LogError(ex, "Provider error from {Provider}: {StatusCode}", "Gemini", ex.StatusCode);
            throw new AiProviderException("Gemini", "Gemini returned an error. Try again shortly.", ex);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error calling {Provider}", "Gemini");
            throw new AiProviderException("Gemini", "Could not reach Gemini. Check your connection and try again.", ex);
        }
    }
}
