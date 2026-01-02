using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Tasks;

public sealed class DeleteTask(ITaskRepository taskRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid TaskId, Guid UserId);

    public async Task<bool> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var task = await taskRepository.GetByIdAsync(command.TaskId, cancellationToken);

        if (task is null || task.UserId != command.UserId)
        {
            return false;
        }

        taskRepository.Remove(task);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
