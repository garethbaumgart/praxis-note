using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tags;

namespace PraxisNote.Application.Features.Tags;

public sealed class CreateTag(ITagRepository tagRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, string Name);
    public record Result(Guid TagId);

    public const string DuplicateNameError = "TAG_DUPLICATE_NAME";

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        // Check for duplicate name
        var existing = await tagRepository.GetByNameAsync(command.UserId, command.Name, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException(DuplicateNameError);
        }

        var tag = Tag.Create(command.UserId, command.Name);

        await tagRepository.AddAsync(tag, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new Result(tag.Id);
    }
}
