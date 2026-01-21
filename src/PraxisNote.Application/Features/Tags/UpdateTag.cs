using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tags;

namespace PraxisNote.Application.Features.Tags;

public sealed class UpdateTag(ITagRepository tagRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid TagId, Guid UserId, string? Name = null, string? Color = null);
    public record Result(bool Success, string? Error = null);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        var tag = await tagRepository.GetByIdAsync(command.TagId, cancellationToken);
        if (tag is null || tag.UserId != command.UserId)
        {
            return new Result(false, Error: "Tag not found");
        }

        // If renaming, check for duplicate name
        if (!string.IsNullOrWhiteSpace(command.Name) && command.Name != tag.Name)
        {
            var existingTag = await tagRepository.GetByNameAsync(command.UserId, command.Name, cancellationToken);
            if (existingTag is not null)
            {
                return new Result(false, Error: "A tag with this name already exists");
            }
            tag.Rename(command.Name);
        }

        // Update color if provided
        if (!string.IsNullOrWhiteSpace(command.Color) && command.Color != tag.Color)
        {
            tag.Recolor(command.Color);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new Result(true);
    }
}
