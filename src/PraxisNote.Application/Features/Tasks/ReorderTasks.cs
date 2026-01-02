using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Tasks;

public sealed class ReorderTasks(ITaskRepository taskRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, string Status, IReadOnlyList<Guid> TaskIds);

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var tasks = await taskRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        var tasksInStatus = tasks
            .Where(t => t.Status.ToString() == command.Status)
            .ToDictionary(t => t.Id);

        for (var i = 0; i < command.TaskIds.Count; i++)
        {
            if (tasksInStatus.TryGetValue(command.TaskIds[i], out var task))
            {
                task.SetPosition(i);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
