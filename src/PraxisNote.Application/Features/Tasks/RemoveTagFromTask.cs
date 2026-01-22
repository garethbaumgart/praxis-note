using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Tasks;

public sealed class RemoveTagFromTask(ITaskRepository taskRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid TaskId, Guid TagId);

    public const string TaskNotFoundError = "TASK_NOT_FOUND";

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var task = await taskRepository.GetByIdAsync(command.TaskId, cancellationToken);
        if (task is null || task.UserId != command.UserId)
        {
            throw new InvalidOperationException(TaskNotFoundError);
        }

        task.RemoveTag(command.TagId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
