using PraxisNote.Domain.Common;

namespace PraxisNote.Domain.Aggregates.Users;

/// <summary>
/// A one-time code used to link another Google account to an existing user.
/// Codes are stored as SHA-256 hashes and expire after a configurable duration.
/// </summary>
public sealed class AccountLinkCode : Entity
{
    /// <summary>
    /// The user who generated this link code.
    /// </summary>
    public Guid UserId { get; private init; }

    /// <summary>
    /// SHA-256 hash of the link code. The plaintext is never stored.
    /// </summary>
    public string CodeHash { get; private init; } = string.Empty;

    /// <summary>
    /// When this code expires and can no longer be redeemed.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; private init; }

    /// <summary>
    /// Whether this code has been redeemed.
    /// </summary>
    public bool IsRedeemed { get; private set; }

    /// <summary>
    /// When this code was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>
    /// Required for EF Core.
    /// </summary>
    private AccountLinkCode() { }

    private AccountLinkCode(Guid id) : base(id) { }

    /// <summary>
    /// Creates a new account link code with a hashed code and expiry duration.
    /// </summary>
    public static AccountLinkCode Create(Guid userId, string codeHash, TimeSpan expiry)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty, nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(codeHash);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(expiry, TimeSpan.Zero, nameof(expiry));

        var now = DateTimeOffset.UtcNow;
        return new AccountLinkCode(Guid.NewGuid())
        {
            UserId = userId,
            CodeHash = codeHash,
            ExpiresAt = now.Add(expiry),
            IsRedeemed = false,
            CreatedAt = now
        };
    }

    /// <summary>
    /// Checks whether this code has expired.
    /// </summary>
    public bool IsExpired() => DateTimeOffset.UtcNow >= ExpiresAt;

    /// <summary>
    /// Checks whether this code is still valid (not redeemed and not expired).
    /// </summary>
    public bool IsValid() => !IsRedeemed && !IsExpired();

    /// <summary>
    /// Marks this code as redeemed so it cannot be used again.
    /// </summary>
    public void MarkRedeemed()
    {
        IsRedeemed = true;
    }
}
