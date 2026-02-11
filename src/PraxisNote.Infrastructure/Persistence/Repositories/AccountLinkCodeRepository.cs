using Microsoft.EntityFrameworkCore;
using PraxisNote.Domain.Aggregates.Users;

namespace PraxisNote.Infrastructure.Persistence.Repositories;

public sealed class AccountLinkCodeRepository(PraxisNoteDbContext context) : IAccountLinkCodeRepository
{
    public async Task<AccountLinkCode?> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await context.AccountLinkCodes
            .Where(c => c.UserId == userId && !c.IsRedeemed)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccountLinkCode>> GetAllActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await context.AccountLinkCodes
            .Where(c => !c.IsRedeemed)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AccountLinkCode code, CancellationToken cancellationToken = default)
    {
        await context.AccountLinkCodes.AddAsync(code, cancellationToken);
    }

    public void Remove(AccountLinkCode code)
    {
        context.AccountLinkCodes.Remove(code);
    }
}
