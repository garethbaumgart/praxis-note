using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tags;
using PraxisNote.Domain.Aggregates.Tasks;

namespace PraxisNote.Application.Features.Tags;

public sealed class DeleteTag(ITagRepository tagRepository, ITaskRepository taskRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid TagId, Guid UserId);

    public async Task<bool> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var tag = await tagRepository.GetByIdAsync(command.TagId, cancellationToken);
        if (tag is null || tag.UserId != command.UserId)
        {
            return false;
        }

        // Remove the tag from all tasks that have it
        var tasks = await taskRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        foreach (var task in tasks.Where(t => t.HasTag(command.TagId)))
        {
            task.RemoveTag(command.TagId);
        }

        tagRepository.Remove(tag);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
