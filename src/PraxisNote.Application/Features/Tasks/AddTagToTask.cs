using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Tasks;

public sealed class AddTagToTask(ITaskRepository taskRepository, ITagRepository tagRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid TaskId, Guid TagId);

    public const string TaskNotFoundError = "TASK_NOT_FOUND";
    public const string TagNotFoundError = "TAG_NOT_FOUND";

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var task = await taskRepository.GetByIdAsync(command.TaskId, cancellationToken);
        if (task is null || task.UserId != command.UserId)
        {
            throw new InvalidOperationException(TaskNotFoundError);
        }

        var tag = await tagRepository.GetByIdAsync(command.TagId, cancellationToken);
        if (tag is null || tag.UserId != command.UserId)
        {
            throw new InvalidOperationException(TagNotFoundError);
        }

        task.AddTag(command.TagId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
