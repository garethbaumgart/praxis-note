using PraxisNote.Application.Common;
using PraxisNote.Application.Features.Tags.Services;
using PraxisNote.Application.Features.UserAiKeys.Services;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Tags;

public sealed class AskTagAi(
    ITagRepository tagRepository,
    IMeetingRepository meetingRepository,
    INoteRepository noteRepository,
    ITaskRepository taskRepository,
    IResolvedAiServices aiServices)
{
    public record Command(
        Guid UserId,
        Guid TagId,
        string Message,
        IReadOnlyList<ChatMessage>? History);

    public const string NotFoundError = "TAG_NOT_FOUND";
    public const string NoContentError = "TAG_NO_CONTENT";

    public async IAsyncEnumerable<string> ExecuteAsync(
        Command command,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var tag = await tagRepository.GetByIdAsync(command.TagId, cancellationToken);
        if (tag is null || tag.UserId != command.UserId)
            throw new InvalidOperationException(NotFoundError);

        // Sequential — DbContext is not thread-safe
        var meetings = await meetingRepository.GetByTagIdAsync(command.UserId, tag.ProfileId, command.TagId, cancellationToken);
        var notes = await noteRepository.GetByTagIdAsync(command.UserId, tag.ProfileId, command.TagId, cancellationToken);
        var tasks = await taskRepository.GetTasksWithTagAsync(command.UserId, tag.ProfileId, command.TagId, cancellationToken);

        if (meetings.Count == 0 && notes.Count == 0 && tasks.Count == 0)
            throw new InvalidOperationException(NoContentError);

        var context = BuildContext(tag.Name, meetings, notes, tasks);
        var history = command.History ?? [];

        var aiChatService = await aiServices.GetTagAiChatServiceAsync(command.UserId, cancellationToken);
        await foreach (var token in aiChatService.StreamResponseAsync(context, command.Message, history, cancellationToken))
        {
            yield return token;
        }
    }

    internal static TagChatContext BuildContext(
        string tagName,
        IReadOnlyList<Meeting> meetings,
        IReadOnlyList<Note> notes,
        IReadOnlyList<TaskItem> tasks)
    {
        var meetingContexts = meetings.Select(m => new TagMeetingContext(
            m.Title ?? "Untitled Meeting",
            m.MeetingDate,
            m.Attendees,
            m.Summary,
            m.TranscriptContent)).ToList();

        var noteContexts = notes.Select(n => new TagNoteContext(
            NoteTitleExtractor.Extract(n.Content),
            TiptapTextExtractor.Extract(n.Content))).ToList();

        var taskContexts = tasks.Select(t => new TagTaskContext(
            t.Title,
            t.Status.ToString(),
            t.IsPriority,
            t.DueDate?.Date)).ToList();

        return new TagChatContext(tagName, meetingContexts, noteContexts, taskContexts);
    }
}
