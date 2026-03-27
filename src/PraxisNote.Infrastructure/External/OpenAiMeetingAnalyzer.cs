using System.ClientModel;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.Meetings.Services;

namespace PraxisNote.Infrastructure.External;

public sealed class OpenAiMeetingAnalyzer(
    ILogger<OpenAiMeetingAnalyzer> logger,
    string apiKey,
    string model,
    int maxTokens = 4096,
    int timeoutSeconds = 120) : IMeetingAnalyzer
{
    private readonly ChatClient _chatClient = new OpenAIClient(new ApiKeyCredential(apiKey))
        .GetChatClient(model);

    public async Task<MeetingAnalysisResult> AnalyzeAsync(string transcript, CancellationToken cancellationToken = default)
    {
        var prompt = AnthropicMeetingAnalyzer.AnalysisPrompt + transcript;

        logger.LogInformation("Starting meeting analysis with OpenAI model {Model}", model);

        var responseText = await SendChatCompletionAsync(
            [ChatMessage.CreateUserMessage(prompt)],
            maxTokens,
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

        logger.LogInformation("Extracting meetings from calendar screenshot with OpenAI model {Model}", model);

        var imageUrl = $"data:{mediaType};base64,{base64Image}";
        var contentParts = new List<ChatMessageContentPart>
        {
            ChatMessageContentPart.CreateImagePart(new Uri(imageUrl)),
            ChatMessageContentPart.CreateTextPart(promptText)
        };

        var responseText = await SendChatCompletionAsync(
            [ChatMessage.CreateUserMessage(contentParts)],
            maxTokens,
            cancellationToken);

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

        logger.LogInformation("Parsing transcript for import with OpenAI model {Model}", model);

        var responseText = await SendChatCompletionAsync(
            [ChatMessage.CreateUserMessage(prompt)],
            maxTokens,
            cancellationToken);

        var result = AnthropicMeetingAnalyzer.ParseTranscriptImportResponse(responseText);
        logger.LogDebug("Transcript import parse result — meetingDate: {MeetingDate}, timezone sent: {TimeZone}",
            result.MeetingDate, tzName);
        return result;
    }

    private async Task<string> SendChatCompletionAsync(
        List<ChatMessage> messages,
        int maxCompletionTokens,
        CancellationToken cancellationToken)
    {
        var options = new ChatCompletionOptions
        {
            MaxOutputTokenCount = maxCompletionTokens,
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        var completion = await _chatClient.CompleteChatAsync(messages, options, cts.Token);

        var content = completion.Value.Content
            .Where(p => p.Kind == ChatMessageContentPartKind.Text)
            .Select(p => p.Text)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("OpenAI returned an empty response");
        }

        return content;
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
}
