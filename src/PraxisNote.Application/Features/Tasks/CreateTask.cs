using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Tasks;

public sealed class CreateTask(ITaskRepository taskRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, string Title);
    public record Result(Guid TaskId);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        // Get existing tasks to increment their positions
        var existingTasks = await taskRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        var todoTasks = existingTasks.Where(t => t.Status == Domain.ValueObjects.TaskStatus.Todo);

        // Push down existing Todo tasks
        foreach (var existingTask in todoTasks)
        {
            existingTask.SetPosition(existingTask.Position + 1);
        }

        // Create new task at position 0
        var task = TaskItem.CreateStandalone(command.UserId, command.Title);

        await taskRepository.AddAsync(task, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new Result(task.Id);
    }
}
