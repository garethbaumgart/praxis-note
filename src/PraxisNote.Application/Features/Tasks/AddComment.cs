using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Tasks;

public sealed class AddComment(ITaskRepository taskRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid TaskId, Guid UserId, string Content);
    public record Result(Guid CommentId);

    public async Task<Result?> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var task = await taskRepository.GetByIdAsync(command.TaskId, cancellationToken);

        if (task is null || task.UserId != command.UserId)
        {
            return null;
        }

        var comment = task.AddComment(command.Content);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new Result(comment.Id);
    }
}
