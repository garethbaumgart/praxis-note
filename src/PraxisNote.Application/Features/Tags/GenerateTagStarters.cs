using PraxisNote.Application.Features.Tags.Services;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Tags;

public sealed class GenerateTagStarters(
    ITagRepository tagRepository,
    IMeetingRepository meetingRepository,
    INoteRepository noteRepository,
    ITaskRepository taskRepository,
    ITagAiChatService aiChatService)
{
    public record Query(Guid UserId, Guid TagId);

    public const string NotFoundError = "TAG_NOT_FOUND";
    public const string NoContentError = "TAG_NO_CONTENT";

    public async Task<IReadOnlyList<string>> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        var tag = await tagRepository.GetByIdAsync(query.TagId, cancellationToken);
        if (tag is null || tag.UserId != query.UserId)
            throw new InvalidOperationException(NotFoundError);

        // Sequential — DbContext is not thread-safe
        var meetings = await meetingRepository.GetByTagIdAsync(query.UserId, tag.ProfileId, query.TagId, cancellationToken);
        var notes = await noteRepository.GetByTagIdAsync(query.UserId, tag.ProfileId, query.TagId, cancellationToken);
        var tasks = await taskRepository.GetTasksWithTagAsync(query.UserId, tag.ProfileId, query.TagId, cancellationToken);

        if (meetings.Count == 0 && notes.Count == 0 && tasks.Count == 0)
            throw new InvalidOperationException(NoContentError);

        var context = AskTagAi.BuildContext(tag.Name, meetings, notes, tasks);

        return await aiChatService.GenerateStarterPromptsAsync(context, cancellationToken);
    }
}
