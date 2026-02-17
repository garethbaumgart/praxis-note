namespace PraxisNote.Application.Features.Tags.Services;

/// <summary>
/// AI chat service scoped to a single tag's content.
/// Provides conversational Q&A with streaming responses.
/// </summary>
public interface ITagAiChatService
{
    /// <summary>
    /// Streams AI response tokens for a user's question about tag content.
    /// </summary>
    IAsyncEnumerable<string> StreamResponseAsync(
        TagChatContext context,
        string userMessage,
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates contextual starter prompts based on the tag's content.
    /// </summary>
    Task<IReadOnlyList<string>> GenerateStarterPromptsAsync(
        TagChatContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Full context for an AI chat session scoped to a tag.
/// </summary>
public record TagChatContext(
    string TagName,
    IReadOnlyList<TagMeetingContext> Meetings,
    IReadOnlyList<TagNoteContext> Notes,
    IReadOnlyList<TagTaskContext> Tasks);

/// <summary>
/// Meeting content for AI context.
/// </summary>
public record TagMeetingContext(
    string Title,
    DateTimeOffset? MeetingDate,
    string? Attendees,
    string? Summary,
    string? Transcript);

/// <summary>
/// Note content for AI context.
/// </summary>
public record TagNoteContext(
    string Title,
    string PlainTextContent);

/// <summary>
/// Task content for AI context.
/// </summary>
public record TagTaskContext(
    string Title,
    string Status,
    bool IsPriority,
    DateOnly? DueDate);

/// <summary>
/// A single message in the chat history.
/// </summary>
public record ChatMessage(
    string Role,   // "user" or "assistant"
    string Content);
