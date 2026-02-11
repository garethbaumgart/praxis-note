using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Tags;

namespace PraxisNote.Application.Features.Tags;

public sealed class CreateTag(ITagRepository tagRepository, IUnitOfWork unitOfWork)
{
    public record Command(Guid UserId, Guid ProfileId, string Name);
    public record Result(Guid TagId);

    public const string DuplicateNameError = "TAG_DUPLICATE_NAME";
    public const string NameTooLongError = "TAG_NAME_TOO_LONG";
    public const int MaxNameLength = 50;

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        // Validate name length
        if (command.Name.Length > MaxNameLength)
        {
            throw new InvalidOperationException(NameTooLongError);
        }

        // Check for duplicate name
        var existing = await tagRepository.GetByNameAsync(command.UserId, command.ProfileId, command.Name, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException(DuplicateNameError);
        }

        var tag = Tag.Create(command.UserId, command.ProfileId, command.Name);

        await tagRepository.AddAsync(tag, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new Result(tag.Id);
    }
}
