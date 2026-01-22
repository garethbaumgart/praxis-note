using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tags;

namespace PraxisNote.Application.Features.Tags;

public sealed class UpdateTag(ITagRepository tagRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid TagId, string Name);

    public const string NotFoundError = "TAG_NOT_FOUND";
    public const string DuplicateNameError = "TAG_DUPLICATE_NAME";

    public async Task ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var tag = await tagRepository.GetByIdAsync(command.TagId, cancellationToken);
        if (tag is null || tag.UserId != command.UserId)
        {
            throw new InvalidOperationException(NotFoundError);
        }

        // Check for duplicate name (exclude current tag)
        var existing = await tagRepository.GetByNameAsync(command.UserId, command.Name, cancellationToken);
        if (existing is not null && existing.Id != command.TagId)
        {
            throw new InvalidOperationException(DuplicateNameError);
        }

        tag.Rename(command.Name);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
