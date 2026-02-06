using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Tags;

public sealed class PreviewTagMerge(
    ITagRepository tagRepository,
    ITaskRepository taskRepository,
    INoteRepository noteRepository,
    IMeetingRepository meetingRepository)
{
    public record Query(Guid UserId, Guid SourceTagId, Guid TargetTagId);

    public record PreviewResult(
        string SourceTagName,
        int SourceTaskCount,
        int SourceNoteCount,
        int SourceMeetingCount,
        string TargetTagName,
        int TargetTaskCount,
        int TargetNoteCount,
        int TargetMeetingCount,
        int ResultTaskCount,
        int ResultNoteCount,
        int ResultMeetingCount,
        int OverlapCount);

    public const string SourceNotFoundError = "TAG_SOURCE_NOT_FOUND";
    public const string TargetNotFoundError = "TAG_TARGET_NOT_FOUND";
    public const string SameTagError = "TAG_MERGE_SAME_TAG";

    public async Task<PreviewResult> ExecuteAsync(Query query, CancellationToken cancellationToken = default)
    {
        if (query.SourceTagId == query.TargetTagId)
            throw new InvalidOperationException(SameTagError);

        var sourceTag = await tagRepository.GetByIdAsync(query.SourceTagId, cancellationToken);
        if (sourceTag is null || sourceTag.UserId != query.UserId)
            throw new InvalidOperationException(SourceNotFoundError);

        var targetTag = await tagRepository.GetByIdAsync(query.TargetTagId, cancellationToken);
        if (targetTag is null || targetTag.UserId != query.UserId)
            throw new InvalidOperationException(TargetNotFoundError);

        // Sequential — DbContext is not thread-safe
        var sourceTasks = await taskRepository.GetTasksWithTagAsync(query.UserId, query.SourceTagId, cancellationToken);
        var targetTasks = await taskRepository.GetTasksWithTagAsync(query.UserId, query.TargetTagId, cancellationToken);

        var sourceNotes = await noteRepository.GetByTagIdAsync(query.UserId, query.SourceTagId, cancellationToken);
        var targetNotes = await noteRepository.GetByTagIdAsync(query.UserId, query.TargetTagId, cancellationToken);

        var sourceMeetings = await meetingRepository.GetByTagIdAsync(query.UserId, query.SourceTagId, cancellationToken);
        var targetMeetings = await meetingRepository.GetByTagIdAsync(query.UserId, query.TargetTagId, cancellationToken);

        // Calculate overlaps (items that have BOTH tags)
        var targetTaskIds = new HashSet<Guid>(targetTasks.Select(t => t.Id));
        var targetNoteIds = new HashSet<Guid>(targetNotes.Select(n => n.Id));
        var targetMeetingIds = new HashSet<Guid>(targetMeetings.Select(m => m.Id));

        var taskOverlap = sourceTasks.Count(t => targetTaskIds.Contains(t.Id));
        var noteOverlap = sourceNotes.Count(n => targetNoteIds.Contains(n.Id));
        var meetingOverlap = sourceMeetings.Count(m => targetMeetingIds.Contains(m.Id));

        return new PreviewResult(
            SourceTagName: sourceTag.Name,
            SourceTaskCount: sourceTasks.Count,
            SourceNoteCount: sourceNotes.Count,
            SourceMeetingCount: sourceMeetings.Count,
            TargetTagName: targetTag.Name,
            TargetTaskCount: targetTasks.Count,
            TargetNoteCount: targetNotes.Count,
            TargetMeetingCount: targetMeetings.Count,
            ResultTaskCount: targetTasks.Count + sourceTasks.Count - taskOverlap,
            ResultNoteCount: targetNotes.Count + sourceNotes.Count - noteOverlap,
            ResultMeetingCount: targetMeetings.Count + sourceMeetings.Count - meetingOverlap,
            OverlapCount: taskOverlap + noteOverlap + meetingOverlap);
    }
}
