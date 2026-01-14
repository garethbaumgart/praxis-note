using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Tasks;

public sealed class ToggleTaskPriority(ITaskRepository taskRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid TaskId, Guid UserId);

    public async Task<bool> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var task = await taskRepository.GetByIdAsync(command.TaskId, cancellationToken);

        if (task is null || task.UserId != command.UserId)
        {
            return false;
        }

        task.TogglePriority();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
