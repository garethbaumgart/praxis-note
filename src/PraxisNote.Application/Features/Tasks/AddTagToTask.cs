using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Tasks;

public sealed class AddTagToTask(ITaskRepository taskRepository, ITagRepository tagRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid TaskId, Guid TagId, Guid UserId);

    public enum ErrorCode { None, TaskNotFound, TagNotFound }

    public record Result(bool Success, ErrorCode Error = ErrorCode.None, string? Message = null);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var task = await taskRepository.GetByIdAsync(command.TaskId, cancellationToken);
        if (task is null || task.UserId != command.UserId)
        {
            return new Result(false, ErrorCode.TaskNotFound, "Task not found");
        }

        var tag = await tagRepository.GetByIdAsync(command.TagId, cancellationToken);
        if (tag is null || tag.UserId != command.UserId)
        {
            return new Result(false, ErrorCode.TagNotFound, "Tag not found");
        }

        task.AddTag(command.TagId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new Result(true);
    }
}
