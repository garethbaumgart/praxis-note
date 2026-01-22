using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tags;

namespace PraxisNote.Application.Features.Tags;

public sealed class CreateTag(ITagRepository tagRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, string Name, string Color);
    public record Result(bool Success, Guid? TagId = null, string? Error = null);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        // Check for duplicate name
        var existingTag = await tagRepository.GetByNameAsync(command.UserId, command.Name, cancellationToken);
        if (existingTag is not null)
        {
            return new Result(false, Error: "A tag with this name already exists");
        }

        var tag = Tag.Create(command.UserId, command.Name, command.Color);
        await tagRepository.AddAsync(tag, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new Result(true, tag.Id);
    }
}
