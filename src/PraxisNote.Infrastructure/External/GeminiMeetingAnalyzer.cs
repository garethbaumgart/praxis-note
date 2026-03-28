using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Application.Features.UserAiKeys;
using static PraxisNote.Infrastructure.External.GeminiJsonConfiguration;

namespace PraxisNote.Infrastructure.External;

public sealed class GeminiMeetingAnalyzer(
    HttpClient httpClient,
    ILogger<GeminiMeetingAnalyzer> logger,
    string apiKey,
    string model,
    int maxTokens = 4096,
    int timeoutSeconds = 120) : IMeetingAnalyzer
{
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    public async Task<MeetingAnalysisResult> AnalyzeAsync(string transcript, CancellationToken cancellationToken = default)
    {
        var prompt = AnthropicMeetingAnalyzer.AnalysisPrompt + transcript;

        logger.LogInformation("Starting meeting analysis with Gemini model {Model}", model);

        var responseText = await SendGenerateContentAsync(
            [new GeminiPart { Text = prompt }],
            cancellationToken);

        return AnthropicMeetingAnalyzer.ParseAnalysisResponse(responseText);
    }

    public async Task<ScreenshotExtractionResult> ExtractFromScreenshotAsync(
        string base64Image, string mediaType, string? timeZone = null, CancellationToken cancellationToken = default)
    {
        var resolved = TimeZoneHelper.ResolveTimeZone(timeZone, logger);
        var userNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, resolved.TimeZoneInfo);
        var baseDate = userNow.ToString("yyyy-MM-dd");
        var offsetExample = userNow.ToString("zzz");

        var promptText = AnthropicMeetingAnalyzer.ScreenshotExtractionPromptTemplate
            .Replace("<<BASE_DATE>>", baseDate)
            .Replace("<<TIMEZONE>>", resolved.DisplayName)
            .Replace("<<OFFSET_EXAMPLE>>", offsetExample);

        logger.LogInformation("Extracting meetings from calendar screenshot with Gemini model {Model}", model);

        var parts = new List<GeminiPart>
        {
            new() { InlineData = new GeminiInlineData { MimeType = mediaType, Data = base64Image } },
            new() { Text = promptText }
        };

        var responseText = await SendGenerateContentAsync(parts, cancellationToken);

        return AnthropicMeetingAnalyzer.ParseScreenshotExtractionResponse(responseText);
    }

    public async Task<TranscriptImportResult> ParseTranscriptForImportAsync(
        string transcript, string? timeZone = null, CancellationToken cancellationToken = default)
    {
        var resolved = TimeZoneHelper.ResolveTimeZone(timeZone, logger);
        var userNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, resolved.TimeZoneInfo);
        var baseDate = userNow.ToString("yyyy-MM-dd");
        var offsetExample = userNow.ToString("zzz");

        var promptText = AnthropicMeetingAnalyzer.TranscriptImportPromptTemplate
            .Replace("<<TIMEZONE>>", resolved.DisplayName)
            .Replace("<<BASE_DATE>>", baseDate)
            .Replace("<<OFFSET_EXAMPLE>>", offsetExample);

        var prompt = promptText + transcript;

        logger.LogInformation("Parsing transcript for import with Gemini model {Model}", model);

        var responseText = await SendGenerateContentAsync(
            [new GeminiPart { Text = prompt }],
            cancellationToken);

        var result = AnthropicMeetingAnalyzer.ParseTranscriptImportResponse(responseText);
        logger.LogDebug("Transcript import parse result — meetingDate: {MeetingDate}, timezone sent: {TimeZone}",
            result.MeetingDate, resolved.DisplayName);
        return result;
    }

    private async Task<string> SendGenerateContentAsync(List<GeminiPart> parts, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/{model}:generateContent";

        var requestBody = new GeminiRequest
        {
            Contents = [new GeminiContent { Parts = parts }],
            GenerationConfig = new GeminiGenerationConfig { MaxOutputTokens = maxTokens }
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(requestBody, options: Options)
            };
            request.Headers.Add("x-goog-api-key", apiKey);

            using var response = await httpClient.SendAsync(request, cts.Token);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfterSeconds = response.Headers.RetryAfter?.Delta is { } delta
                    ? (int)Math.Ceiling(delta.TotalSeconds)
                    : response.Headers.RetryAfter?.Date is { } date
                        ? Math.Max(0, (int)Math.Ceiling((date - DateTimeOffset.UtcNow).TotalSeconds))
                        : (int?)null;
                logger.LogWarning("Rate limited by {Provider}", "Gemini");
                throw new AiRateLimitedException("Gemini", retryAfterSeconds);
            }
            response.EnsureSuccessStatusCode();

            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(Options, cts.Token)
                ?? throw new InvalidOperationException("Gemini returned an empty response");

            var text = string.Concat(
                geminiResponse.Candidates?
                    .SelectMany(c => c.Content?.Parts ?? [])
                    .Select(p => p.Text ?? string.Empty) ?? []);

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("Gemini returned an empty response");
            }

            return text;
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
