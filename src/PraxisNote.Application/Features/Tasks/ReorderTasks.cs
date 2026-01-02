using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tasks;
using TaskStatus = PraxisNote.Domain.ValueObjects.TaskStatus;

namespace PraxisNote.Application.Features.Tasks;

public sealed class ReorderTasks(ITaskRepository taskRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, string Status, IReadOnlyList<Guid> TaskIds);
    public record Result(bool Success, string? Error = null);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        // Validate status
        if (!Enum.TryParse<TaskStatus>(command.Status, out var status))
        {
            return new Result(false, $"Invalid status: {command.Status}");
        }

        // Done tasks are sorted by completion time, not position
        if (status == TaskStatus.Done)
        {
            return new Result(false, "Cannot reorder tasks in Done status. Done tasks are sorted by completion time.");
        }

        var tasks = await taskRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        var tasksInStatus = tasks
            .Where(t => t.Status == status)
            .ToDictionary(t => t.Id);

        // Validate all TaskIds belong to the user and are in the specified status
        var invalidIds = command.TaskIds.Where(id => !tasksInStatus.ContainsKey(id)).ToList();
        if (invalidIds.Count > 0)
        {
            return new Result(false, "One or more task IDs are invalid or do not belong to the user");
        }

        for (var i = 0; i < command.TaskIds.Count; i++)
        {
            if (tasksInStatus.TryGetValue(command.TaskIds[i], out var task))
            {
                task.SetPosition(i);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new Result(true);
    }
}
