using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Meetings;
using PraxisNote.Domain.Aggregates.Notes;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Tags;

public sealed class DeleteTag(
    ITagRepository tagRepository,
    ITaskRepository taskRepository,
    INoteRepository noteRepository,
    IMeetingRepository meetingRepository,
    IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid TagId);

    public const string NotFoundError = "TAG_NOT_FOUND";

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var tag = await tagRepository.GetByIdAsync(command.TagId, cancellationToken);
        if (tag is null || tag.UserId != command.UserId)
        {
            throw new InvalidOperationException(NotFoundError);
        }

        // Sequential — EF Core DbContext is not thread-safe
        var tasksWithTag = await taskRepository.GetTasksWithTagAsync(command.UserId, command.TagId, cancellationToken);
        foreach (var task in tasksWithTag)
            task.RemoveTag(command.TagId);

        var notesWithTag = await noteRepository.GetByTagIdAsync(command.UserId, command.TagId, cancellationToken);
        foreach (var note in notesWithTag)
            note.RemoveTag(command.TagId);

        var meetingsWithTag = await meetingRepository.GetByTagIdAsync(command.UserId, command.TagId, cancellationToken);
        foreach (var meeting in meetingsWithTag)
            meeting.RemoveTag(command.TagId);

        tagRepository.Remove(tag);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
