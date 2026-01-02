using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tasks;

using TaskStatus = PraxisNote.Domain.ValueObjects.TaskStatus;

namespace PraxisNote.Application.Features.Tasks;

public sealed class ChangeTaskStatus(ITaskRepository taskRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid TaskId, Guid UserId, string TargetStatus);

    public async Task<bool> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var task = await taskRepository.GetByIdAsync(command.TaskId, cancellationToken);

        if (task is null || task.UserId != command.UserId)
        {
            return false;
        }

        if (!Enum.TryParse<TaskStatus>(command.TargetStatus, ignoreCase: true, out var targetStatus))
        {
            return false;
        }

        // Push down tasks in target column
        var allTasks = await taskRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        var tasksInTargetColumn = allTasks.Where(t => t.Status == targetStatus && t.Id != task.Id);
        foreach (var t in tasksInTargetColumn)
        {
            t.SetPosition(t.Position + 1);
        }

        // Move task to new column at position 0
        task.SetPosition(0);
        switch (targetStatus)
        {
            case TaskStatus.Todo:
                task.Reopen();
                break;
            case TaskStatus.InProgress:
                task.Start();
                break;
            case TaskStatus.Done:
                task.Complete();
                break;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
