using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.Meetings.Services;

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
        var tz = GetTimeZoneInfo(timeZone);
        var userNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
        var baseDate = userNow.ToString("yyyy-MM-dd");
        var tzName = !string.IsNullOrWhiteSpace(timeZone) ? timeZone : tz.Id;
        var offsetExample = userNow.ToString("zzz");

        var promptText = AnthropicMeetingAnalyzer.ScreenshotExtractionPromptTemplate
            .Replace("<<BASE_DATE>>", baseDate)
            .Replace("<<TIMEZONE>>", tzName)
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
        var tz = GetTimeZoneInfo(timeZone);
        var userNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
        var baseDate = userNow.ToString("yyyy-MM-dd");
        var tzName = !string.IsNullOrWhiteSpace(timeZone) ? timeZone : tz.Id;
        var offsetExample = userNow.ToString("zzz");

        var promptText = AnthropicMeetingAnalyzer.TranscriptImportPromptTemplate
            .Replace("<<TIMEZONE>>", tzName)
            .Replace("<<BASE_DATE>>", baseDate)
            .Replace("<<OFFSET_EXAMPLE>>", offsetExample);

        var prompt = promptText + transcript;

        logger.LogInformation("Parsing transcript for import with Gemini model {Model}", model);

        var responseText = await SendGenerateContentAsync(
            [new GeminiPart { Text = prompt }],
            cancellationToken);

        var result = AnthropicMeetingAnalyzer.ParseTranscriptImportResponse(responseText);
        logger.LogDebug("Transcript import parse result — meetingDate: {MeetingDate}, timezone sent: {TimeZone}",
            result.MeetingDate, tzName);
        return result;
    }

    private async Task<string> SendGenerateContentAsync(List<GeminiPart> parts, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/{model}:generateContent?key={apiKey}";

        var requestBody = new GeminiRequest
        {
            Contents = [new GeminiContent { Parts = parts }],
            GenerationConfig = new GeminiGenerationConfig { MaxOutputTokens = maxTokens }
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        var response = await httpClient.PostAsJsonAsync(url, requestBody, GeminiJsonOptions, cts.Token);
        response.EnsureSuccessStatusCode();

        var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(GeminiJsonOptions, cts.Token)
            ?? throw new InvalidOperationException("Gemini returned an empty response");

        var text = geminiResponse.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Gemini returned an empty response");
        }

        return text;
    }

    private TimeZoneInfo GetTimeZoneInfo(string? ianaTimeZone)
    {
        if (string.IsNullOrWhiteSpace(ianaTimeZone))
            return TimeZoneInfo.Local;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(ianaTimeZone);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            logger.LogWarning("Timezone '{TimeZone}' not found, falling back to local timezone", ianaTimeZone);
            return TimeZoneInfo.Local;
        }
    }

    #region Gemini JSON Models

    private static readonly JsonSerializerOptions GeminiJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    internal sealed class GeminiRequest
    {
        public List<GeminiContent> Contents { get; set; } = [];
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    internal sealed class GeminiContent
    {
        public string? Role { get; set; }
        public List<GeminiPart> Parts { get; set; } = [];
    }

    internal sealed class GeminiPart
    {
        public string? Text { get; set; }
        public GeminiInlineData? InlineData { get; set; }
    }

    internal sealed class GeminiInlineData
    {
        public string MimeType { get; set; } = "";
        public string Data { get; set; } = "";
    }

    internal sealed class GeminiGenerationConfig
    {
        public int MaxOutputTokens { get; set; }
    }

    internal sealed class GeminiResponse
    {
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    internal sealed class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
    }

    #endregion
}
