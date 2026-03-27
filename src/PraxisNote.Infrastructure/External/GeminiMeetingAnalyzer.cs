using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.Meetings.Services;
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

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(requestBody, options: Options)
        };
        request.Headers.Add("x-goog-api-key", apiKey);

        using var response = await httpClient.SendAsync(request, cts.Token);
        response.EnsureSuccessStatusCode();

        var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(Options, cts.Token)
            ?? throw new InvalidOperationException("Gemini returned an empty response");

        var text = string.Concat(
            geminiResponse.Candidates?.FirstOrDefault()?.Content?.Parts?
                .Select(p => p.Text ?? string.Empty) ?? []);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Gemini returned an empty response");
        }

        return text;
    }
}
