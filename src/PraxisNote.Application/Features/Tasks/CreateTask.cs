using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Tasks;

public sealed class CreateTask(ITaskRepository taskRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, string Title);
    public record Result(Guid TaskId);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var task = TaskItem.CreateStandalone(command.UserId, command.Title);

        await taskRepository.AddAsync(task, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new Result(task.Id);
    }
}
