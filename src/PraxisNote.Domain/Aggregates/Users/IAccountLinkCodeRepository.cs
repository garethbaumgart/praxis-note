namespace PraxisNote.Domain.Aggregates.Users;

public interface IAccountLinkCodeRepository
{
    Task<AccountLinkCode?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AccountLinkCode?> GetByHashAsync(string codeHash, CancellationToken cancellationToken = default);
    Task AddAsync(AccountLinkCode code, CancellationToken cancellationToken = default);
    void Remove(AccountLinkCode code);
}
