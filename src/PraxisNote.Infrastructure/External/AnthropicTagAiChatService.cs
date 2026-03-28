using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.Tags.Services;
using PraxisNote.Application.Features.UserAiKeys;

namespace PraxisNote.Infrastructure.External;

public sealed class AnthropicTagAiChatService : ITagAiChatService
{
    private readonly AiProviderSettings _settings;
    private readonly ILogger<AnthropicTagAiChatService> _logger;
    private readonly AnthropicClient? _client;
    private readonly string _model;

    internal const string SystemPromptTemplate = """
        You are a helpful assistant for PraxisNote, a professional note-taking and meeting management application.
        You are answering questions about content tagged with "{0}".

        Here is all the content associated with this tag:

        {1}

        GUIDELINES:
        - Answer questions based on the provided content above
        - Be concise and direct in your responses
        - If the answer is not in the provided content, say so clearly
        - Reference specific meetings, notes, or tasks when relevant
        - Use markdown formatting for readability (bold, lists, etc.)
        - Do not make up information that is not in the content
        """;

    internal const string StarterPrompt = """
        Based on the following content tagged with "{0}", generate exactly 4 short, natural-sounding starter questions that a user might want to ask about this content. The questions should be diverse and cover different aspects of the content.

        Content:
        {1}

        Respond ONLY with a valid JSON array of 4 strings. Example: ["Question 1?", "Question 2?", "Question 3?", "Question 4?"]
        """;

    public AnthropicTagAiChatService(IOptions<AiProviderSettings> settings, ILogger<AnthropicTagAiChatService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _model = _settings.Anthropic.DefaultModel;

        if (!string.IsNullOrWhiteSpace(_settings.Anthropic.ApiKey))
        {
            _client = new AnthropicClient(_settings.Anthropic.ApiKey);
        }
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(
        TagChatContext context,
        string userMessage,
        IReadOnlyList<ChatMessage> history,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_client is null)
        {
            throw new InvalidOperationException(
                "Anthropic API key is not configured. Set AiProviders:Anthropic:ApiKey in appsettings or environment variables.");
        }

        var contextBlock = BuildContextBlock(context);
        var systemPrompt = SystemPromptTemplate
            .Replace("{0}", context.TagName)
            .Replace("{1}", contextBlock);

        var messages = new List<Message>();

        // Add conversation history
        foreach (var msg in history)
        {
            messages.Add(new Message(
                msg.Role == "user" ? RoleType.User : RoleType.Assistant,
                msg.Content));
        }

        // Add the current user message
        messages.Add(new Message(RoleType.User, userMessage));

        var parameters = new MessageParameters
        {
            Model = _model,
            MaxTokens = _settings.MaxTokens,
            Messages = messages,
            System = [new SystemMessage(systemPrompt)],
            Stream = true
        };

        _logger.LogDebug("Starting tag AI chat stream with model {Model}", _model);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

        IAsyncEnumerable<MessageResponse> stream;
        try
        {
            stream = _client.Messages.StreamClaudeMessageAsync(parameters, cts.Token);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            _logger.LogError(ex, "AI key rejected by {Provider}", "Anthropic");
            throw new AiKeyInvalidException("Anthropic");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning("Rate limited by {Provider}", "Anthropic");
            throw new AiRateLimitedException("Anthropic");
        }
        catch (HttpRequestException ex) when (ex.StatusCode is { } s && (int)s >= 500)
        {
            _logger.LogError(ex, "Provider error from {Provider}: {StatusCode}", "Anthropic", ex.StatusCode);
            throw new AiProviderException("Anthropic", "Anthropic returned an error. Try again shortly.", ex);
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
                catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    _logger.LogError(ex, "AI key rejected by {Provider}", "Anthropic");
                    throw new AiKeyInvalidException("Anthropic");
                }
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("Rate limited by {Provider}", "Anthropic");
                    throw new AiRateLimitedException("Anthropic");
                }
                catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Timeout calling {Provider}", "Anthropic");
                    throw new AiProviderException("Anthropic", "Anthropic is not responding. Try again shortly.", ex);
                }
                catch (HttpRequestException ex) when (ex.StatusCode is { } s && (int)s >= 500)
                {
                    _logger.LogError(ex, "Provider error from {Provider}: {StatusCode}", "Anthropic", ex.StatusCode);
                    throw new AiProviderException("Anthropic", "Anthropic returned an error. Try again shortly.", ex);
                }

                if (enumerator.Current.Delta?.Text is { } text)
                {
                    yield return text;
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
        if (_client is null)
        {
            throw new InvalidOperationException(
                "Anthropic API key is not configured. Set AiProviders:Anthropic:ApiKey in appsettings or environment variables.");
        }

        var contextBlock = BuildContextBlock(context);
        var prompt = StarterPrompt
            .Replace("{0}", context.TagName)
            .Replace("{1}", contextBlock);

        var parameters = new MessageParameters
        {
            Model = _model,
            MaxTokens = 512,
            Messages = [new Message(RoleType.User, prompt)]
        };

        _logger.LogDebug("Generating starter prompts with model {Model}", _model);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

        try
        {
            var response = await _client.Messages.GetClaudeMessageAsync(parameters, cts.Token);
            var content = response.Content.OfType<TextContent>().FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(content))
            {
                return DefaultStarters(context.TagName);
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
                _logger.LogWarning(ex, "Failed to parse starter prompts JSON, using defaults");
            }

            return DefaultStarters(context.TagName);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            _logger.LogError(ex, "AI key rejected by {Provider}", "Anthropic");
            throw new AiKeyInvalidException("Anthropic");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning("Rate limited by {Provider}", "Anthropic");
            throw new AiRateLimitedException("Anthropic");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout calling {Provider}", "Anthropic");
            throw new AiProviderException("Anthropic", "Anthropic is not responding. Try again shortly.", ex);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is { } s && (int)s >= 500)
        {
            _logger.LogError(ex, "Provider error from {Provider}: {StatusCode}", "Anthropic", ex.StatusCode);
            throw new AiProviderException("Anthropic", "Anthropic returned an error. Try again shortly.", ex);
        }
    }

    internal static string BuildContextBlock(TagChatContext context)
    {
        var sb = new StringBuilder();

        if (context.Meetings.Count > 0)
        {
            sb.AppendLine("## Meetings");
            foreach (var meeting in context.Meetings)
            {
                sb.AppendLine($"### {meeting.Title}");
                if (meeting.MeetingDate.HasValue)
                    sb.AppendLine($"Date: {meeting.MeetingDate.Value:yyyy-MM-dd}");
                if (!string.IsNullOrWhiteSpace(meeting.Attendees))
                    sb.AppendLine($"Attendees: {meeting.Attendees}");
                if (!string.IsNullOrWhiteSpace(meeting.Summary))
                    sb.AppendLine($"Summary: {meeting.Summary}");
                if (!string.IsNullOrWhiteSpace(meeting.Transcript))
                    sb.AppendLine($"Transcript:\n{meeting.Transcript}");
                sb.AppendLine();
            }
        }

        if (context.Notes.Count > 0)
        {
            sb.AppendLine("## Notes");
            foreach (var note in context.Notes)
            {
                sb.AppendLine($"### {note.Title}");
                if (!string.IsNullOrWhiteSpace(note.PlainTextContent))
                    sb.AppendLine(note.PlainTextContent);
                sb.AppendLine();
            }
        }

        if (context.Tasks.Count > 0)
        {
            sb.AppendLine("## Tasks");
            foreach (var task in context.Tasks)
            {
                var priority = task.IsPriority ? " [PRIORITY]" : "";
                var due = task.DueDate.HasValue ? $" (due: {task.DueDate.Value:yyyy-MM-dd})" : "";
                sb.AppendLine($"- [{task.Status}]{priority} {task.Title}{due}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    internal static IReadOnlyList<string> DefaultStarters(string tagName)
    {
        return new List<string>
        {
            $"What are the key themes in my {tagName} content?",
            $"Summarize my recent {tagName} meetings",
            $"What tasks are still outstanding for {tagName}?",
            $"What decisions have been made about {tagName}?"
        };
    }
}
