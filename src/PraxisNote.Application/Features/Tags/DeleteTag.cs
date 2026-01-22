using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Tags;

public sealed class DeleteTag(ITagRepository tagRepository, ITaskRepository taskRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid TagId);

    public const string NotFoundError = "TAG_NOT_FOUND";

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var tag = await tagRepository.GetByIdAsync(command.TagId, cancellationToken);
        if (tag is null || tag.UserId != command.UserId)
        {
            throw new InvalidOperationException(NotFoundError);
        }

        // Remove tag from all tasks
        var tasks = await taskRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        foreach (var task in tasks)
        {
            task.RemoveTag(command.TagId);
        }

        tagRepository.Remove(tag);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
