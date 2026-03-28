using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using PraxisNote.Application.Features.Tags.Services;
using PraxisNote.Application.Features.UserAiKeys;
using OaiChatMessage = OpenAI.Chat.ChatMessage;

namespace PraxisNote.Infrastructure.External;

public sealed class OpenAiTagAiChatService(
    ILogger<OpenAiTagAiChatService> logger,
    string apiKey,
    string model,
    int maxTokens = 4096,
    int timeoutSeconds = 120) : ITagAiChatService
{
    private readonly ChatClient _chatClient = new OpenAIClient(new ApiKeyCredential(apiKey))
        .GetChatClient(model);

    public async IAsyncEnumerable<string> StreamResponseAsync(
        TagChatContext context,
        string userMessage,
        IReadOnlyList<PraxisNote.Application.Features.Tags.Services.ChatMessage> history,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var contextBlock = AnthropicTagAiChatService.BuildContextBlock(context);
        var systemPrompt = AnthropicTagAiChatService.SystemPromptTemplate
            .Replace("{0}", context.TagName)
            .Replace("{1}", contextBlock);

        var messages = new List<OaiChatMessage>
        {
            OaiChatMessage.CreateSystemMessage(systemPrompt)
        };

        // Add conversation history
        foreach (var msg in history)
        {
            messages.Add(msg.Role == "user"
                ? OaiChatMessage.CreateUserMessage(msg.Content)
                : OaiChatMessage.CreateAssistantMessage(msg.Content));
        }

        // Add the current user message
        messages.Add(OaiChatMessage.CreateUserMessage(userMessage));

        var options = new ChatCompletionOptions
        {
            MaxOutputTokenCount = maxTokens,
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        logger.LogDebug("Starting tag AI chat stream with OpenAI model {Model}", model);

        IAsyncEnumerable<StreamingChatCompletionUpdate> stream;
        try
        {
            stream = _chatClient.CompleteChatStreamingAsync(messages, options, cts.Token);
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
        catch (ClientResultException ex) when (ex.Status >= 500)
        {
            logger.LogError(ex, "Provider error from {Provider}: {StatusCode}", "OpenAI", ex.Status);
            throw new AiProviderException("OpenAI", "OpenAI returned an error. Try again shortly.", ex);
        }
        catch (ClientResultException ex)
        {
            logger.LogError(ex, "Unexpected error from {Provider}: {StatusCode}", "OpenAI", ex.Status);
            throw new AiProviderException("OpenAI", "Could not reach OpenAI. Check your connection and try again.", ex);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error calling {Provider}", "OpenAI");
            throw new AiProviderException("OpenAI", "Could not reach OpenAI. Check your connection and try again.", ex);
        }

        var enumerator = stream.GetAsyncEnumerator(cts.Token);
        try
        {
            while (true)
            {
                try
                {
                    if (!await enumerator.MoveNextAsync())
                        break;
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
                catch (ClientResultException ex)
                {
                    logger.LogError(ex, "Unexpected error from {Provider}: {StatusCode}", "OpenAI", ex.Status);
                    throw new AiProviderException("OpenAI", "Could not reach OpenAI. Check your connection and try again.", ex);
                }
                catch (HttpRequestException ex)
                {
                    logger.LogError(ex, "Network error calling {Provider}", "OpenAI");
                    throw new AiProviderException("OpenAI", "Could not reach OpenAI. Check your connection and try again.", ex);
                }

                foreach (var part in enumerator.Current.ContentUpdate)
                {
                    if (part.Kind == ChatMessageContentPartKind.Text && !string.IsNullOrEmpty(part.Text))
                    {
                        yield return part.Text;
                    }
                }
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
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

        var options = new ChatCompletionOptions
        {
            MaxOutputTokenCount = 512,
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        logger.LogDebug("Generating starter prompts with OpenAI model {Model}", model);

        try
        {
            var completion = await _chatClient.CompleteChatAsync(
                [OaiChatMessage.CreateUserMessage(prompt)], options, cts.Token);

            var content = string.Concat(
                completion.Value.Content
                    .Where(p => p.Kind == ChatMessageContentPartKind.Text && !string.IsNullOrEmpty(p.Text))
                    .Select(p => p.Text));

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
                logger.LogWarning(ex, "Failed to parse OpenAI starter prompts JSON, using defaults");
            }

            return AnthropicTagAiChatService.DefaultStarters(context.TagName);
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
        catch (ClientResultException ex)
        {
            logger.LogError(ex, "Unexpected error from {Provider}: {StatusCode}", "OpenAI", ex.Status);
            throw new AiProviderException("OpenAI", "Could not reach OpenAI. Check your connection and try again.", ex);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error calling {Provider}", "OpenAI");
            throw new AiProviderException("OpenAI", "Could not reach OpenAI. Check your connection and try again.", ex);
        }
    }
}
