using System.ClientModel;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.Meetings.Services;
using PraxisNote.Application.Features.UserAiKeys;

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
        var resolved = TimeZoneHelper.ResolveTimeZone(timeZone, logger);
        var userNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, resolved.TimeZoneInfo);
        var baseDate = userNow.ToString("yyyy-MM-dd");
        var offsetExample = userNow.ToString("zzz");

        var promptText = AnthropicMeetingAnalyzer.ScreenshotExtractionPromptTemplate
            .Replace("<<BASE_DATE>>", baseDate)
            .Replace("<<TIMEZONE>>", resolved.DisplayName)
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
        var resolved = TimeZoneHelper.ResolveTimeZone(timeZone, logger);
        var userNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, resolved.TimeZoneInfo);
        var baseDate = userNow.ToString("yyyy-MM-dd");
        var offsetExample = userNow.ToString("zzz");

        var promptText = AnthropicMeetingAnalyzer.TranscriptImportPromptTemplate
            .Replace("<<TIMEZONE>>", resolved.DisplayName)
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
            result.MeetingDate, resolved.DisplayName);
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

        try
        {
            var completion = await _chatClient.CompleteChatAsync(messages, options, cts.Token);

            var content = string.Concat(
                completion.Value.Content
                    .Where(p => p.Kind == ChatMessageContentPartKind.Text)
                    .Select(p => p.Text));

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("OpenAI returned an empty response");
            }

            return content;
        }
        catch (ClientResultException ex) when (ex.Status is 401 or 403)
        {
            logger.LogError(ex, "AI key rejected by {Provider}", "OpenAI");
            throw new AiKeyInvalidException("OpenAI");
        }
        catch (ClientResultException ex) when (ex.Status == 429)
        {
            logger.LogWarning("Rate limited by {Provider}", "OpenAI");
            throw new AiRateLimitedException("OpenAI");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Timeout calling {Provider}", "OpenAI");
            throw new AiProviderException("OpenAI", "OpenAI is not responding. Try again shortly.", ex);
        }
        catch (ClientResultException ex) when (ex.Status >= 500)
        {
            logger.LogError(ex, "Provider error from {Provider}: {StatusCode}", "OpenAI", ex.Status);
            throw new AiProviderException("OpenAI", "OpenAI returned an error. Try again shortly.", ex);
        }
    }
}
