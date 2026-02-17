using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PraxisNote.Application.Features.Meetings;
using PraxisNote.Application.Features.Tags.Services;

namespace PraxisNote.Infrastructure.External;

public sealed class ClaudeTagAiChatService : ITagAiChatService
{
    private readonly MeetingAnalysisSettings _settings;
    private readonly ILogger<ClaudeTagAiChatService> _logger;
    private readonly AnthropicClient? _client;

    private const string SystemPromptTemplate = """
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

    private const string StarterPrompt = """
        Based on the following content tagged with "{0}", generate exactly 4 short, natural-sounding starter questions that a user might want to ask about this content. The questions should be diverse and cover different aspects of the content.

        Content:
        {1}

        Respond ONLY with a valid JSON array of 4 strings. Example: ["Question 1?", "Question 2?", "Question 3?", "Question 4?"]
        """;

    public ClaudeTagAiChatService(IOptions<MeetingAnalysisSettings> settings, ILogger<ClaudeTagAiChatService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _client = new AnthropicClient(_settings.ApiKey);
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
                "Anthropic API key is not configured. Set MeetingAnalysis:ApiKey in appsettings or environment variables.");
        }

        var contextBlock = BuildContextBlock(context);
        var systemPrompt = string.Format(SystemPromptTemplate, context.TagName, contextBlock);

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
            Model = _settings.Model,
            MaxTokens = _settings.MaxTokens,
            Messages = messages,
            System = [new SystemMessage(systemPrompt)],
            Stream = true
        };

        _logger.LogInformation("Starting tag AI chat stream for tag '{TagName}' with model {Model}",
            context.TagName, _settings.Model);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

        await foreach (var response in _client.Messages.StreamClaudeMessageAsync(parameters, cts.Token))
        {
            if (response.Delta?.Text is { } text)
            {
                yield return text;
            }
        }
    }

    public async Task<IReadOnlyList<string>> GenerateStarterPromptsAsync(
        TagChatContext context,
        CancellationToken cancellationToken = default)
    {
        if (_client is null)
        {
            throw new InvalidOperationException(
                "Anthropic API key is not configured. Set MeetingAnalysis:ApiKey in appsettings or environment variables.");
        }

        var contextBlock = BuildContextBlock(context);
        var prompt = string.Format(StarterPrompt, context.TagName, contextBlock);

        var parameters = new MessageParameters
        {
            Model = _settings.Model,
            MaxTokens = 512,
            Messages = [new Message(RoleType.User, prompt)]
        };

        _logger.LogInformation("Generating starter prompts for tag '{TagName}'", context.TagName);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

        var response = await _client.Messages.GetClaudeMessageAsync(parameters, cts.Token);
        var content = response.Content.OfType<TextContent>().FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(content))
        {
            return DefaultStarters(context.TagName);
        }

        try
        {
            var cleanJson = CleanJsonResponse(content);
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

    private static string BuildContextBlock(TagChatContext context)
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

    private static IReadOnlyList<string> DefaultStarters(string tagName)
    {
        return new List<string>
        {
            $"What are the key themes in my {tagName} content?",
            $"Summarize my recent {tagName} meetings",
            $"What tasks are still outstanding for {tagName}?",
            $"What decisions have been made about {tagName}?"
        };
    }

    private static string CleanJsonResponse(string jsonResponse)
    {
        var cleanJson = jsonResponse.Trim();
        var hadCodeBlock = false;
        if (cleanJson.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleanJson = cleanJson[7..];
            hadCodeBlock = true;
        }
        else if (cleanJson.StartsWith("```"))
        {
            cleanJson = cleanJson[3..];
            hadCodeBlock = true;
        }
        if (hadCodeBlock && cleanJson.EndsWith("```"))
            cleanJson = cleanJson[..^3];
        return cleanJson.Trim();
    }
}
