using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tasks;
using PraxisNote.Domain.ValueObjects;

namespace PraxisNote.Application.Features.Tasks;

public sealed class SetDueDate(ITaskRepository taskRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid TaskId, Guid UserId, DateOnly Date);

    public async Task<bool> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var task = await taskRepository.GetByIdAsync(command.TaskId, cancellationToken);

        if (task is null || task.UserId != command.UserId)
        {
            return false;
        }

        task.SetDueDate(new DueDate(command.Date));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
