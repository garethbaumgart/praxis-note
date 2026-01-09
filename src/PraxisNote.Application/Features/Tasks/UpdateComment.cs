using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Tasks;

public sealed class UpdateComment(ITaskRepository taskRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid TaskId, Guid CommentId, Guid UserId, string Content);

    public async Task<bool> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var task = await taskRepository.GetByIdAsync(command.TaskId, cancellationToken);

        if (task is null || task.UserId != command.UserId)
        {
            return false;
        }

        var updated = task.UpdateComment(command.CommentId, command.Content);
        if (updated is null)
        {
            return false;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
