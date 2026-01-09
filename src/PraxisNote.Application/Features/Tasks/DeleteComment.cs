using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Tasks;

public sealed class DeleteComment(ITaskRepository taskRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid TaskId, Guid CommentId, Guid UserId);

    public async Task<bool> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var task = await taskRepository.GetByIdAsync(command.TaskId, cancellationToken);

        if (task is null || task.UserId != command.UserId)
        {
            return false;
        }

        var removed = task.RemoveComment(command.CommentId);
        if (!removed)
        {
            return false;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
