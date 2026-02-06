using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Tags;

public sealed class MergeTags(
    ITagRepository tagRepository,
    ITaskRepository taskRepository,
    INoteRepository noteRepository,
    IMeetingRepository meetingRepository,
    IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid SourceTagId, Guid TargetTagId);
    public record Result(int TaskCount, int NoteCount, int MeetingCount, int TotalCount);

    public const string SourceNotFoundError = "TAG_SOURCE_NOT_FOUND";
    public const string TargetNotFoundError = "TAG_TARGET_NOT_FOUND";
    public const string SameTagError = "TAG_MERGE_SAME_TAG";

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        if (command.SourceTagId == command.TargetTagId)
            throw new InvalidOperationException(SameTagError);

        var sourceTag = await tagRepository.GetByIdAsync(command.SourceTagId, cancellationToken);
        if (sourceTag is null || sourceTag.UserId != command.UserId)
            throw new InvalidOperationException(SourceNotFoundError);

        var targetTag = await tagRepository.GetByIdAsync(command.TargetTagId, cancellationToken);
        if (targetTag is null || targetTag.UserId != command.UserId)
            throw new InvalidOperationException(TargetNotFoundError);

        // Sequential — EF Core DbContext is not thread-safe
        var tasks = await taskRepository.GetTasksWithTagAsync(command.UserId, command.SourceTagId, cancellationToken);
        foreach (var task in tasks)
        {
            task.AddTag(command.TargetTagId);
            task.RemoveTag(command.SourceTagId);
        }

        var notes = await noteRepository.GetByTagIdAsync(command.UserId, command.SourceTagId, cancellationToken);
        foreach (var note in notes)
        {
            note.AddTag(command.TargetTagId);
            note.RemoveTag(command.SourceTagId);
        }

        var meetings = await meetingRepository.GetByTagIdAsync(command.UserId, command.SourceTagId, cancellationToken);
        foreach (var meeting in meetings)
        {
            meeting.AddTag(command.TargetTagId);
            meeting.RemoveTag(command.SourceTagId);
        }

        tagRepository.Remove(sourceTag);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new Result(tasks.Count, notes.Count, meetings.Count, tasks.Count + notes.Count + meetings.Count);
    }
}
