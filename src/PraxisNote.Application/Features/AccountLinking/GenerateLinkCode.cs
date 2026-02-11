using PraxisNote.Application.Common;
using PraxisNote.Domain.Aggregates.Users;

namespace PraxisNote.Application.Features.AccountLinking;

public sealed class GenerateLinkCode(
    IAccountLinkCodeRepository accountLinkCodeRepository,
    IUnitOfWork unitOfWork)
{
    private static readonly TimeSpan CodeExpiry = TimeSpan.FromMinutes(15);

    public record Command(Guid UserId);
    public record Result(string Code, DateTimeOffset ExpiresAt);

    public async Task<Result> ExecuteAsync(Command command, CancellationToken cancellationToken = default)
    {
        // Invalidate any existing active code for this user
        var existingCode = await accountLinkCodeRepository.GetActiveByUserIdAsync(
            command.UserId, cancellationToken);

        if (existingCode is not null)
        {
            existingCode.MarkRedeemed();
        }

        // Generate new code
        var plainTextCode = LinkCodeService.GenerateCode();
        var codeHash = LinkCodeService.HashCode(plainTextCode);

        var linkCode = AccountLinkCode.Create(command.UserId, codeHash, CodeExpiry);

        await accountLinkCodeRepository.AddAsync(linkCode, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new Result(plainTextCode, linkCode.ExpiresAt);
    }
}
