using System.Security.Cryptography;
using System.Text;
using PraxisNote.Domain.Common;

namespace PraxisNote.Domain.Aggregates.ApiKeys;

public sealed class ApiKey : AggregateRoot
{
    public Guid UserId { get; private init; }
    public Guid ProfileId { get; private init; }
    public string Name { get; private set; } = string.Empty;
    public string KeyHash { get; private init; } = string.Empty;
    public string KeyPrefix { get; private init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset? LastUsedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private init; }
    public bool IsRevoked { get; private set; }

    private ApiKey() { } // EF Core

    private ApiKey(Guid id, Guid userId, Guid profileId, string name,
        string keyHash, string keyPrefix, DateTimeOffset? expiresAt) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty, nameof(userId));
        ArgumentOutOfRangeException.ThrowIfEqual(profileId, Guid.Empty, nameof(profileId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        UserId = userId;
        ProfileId = profileId;
        Name = name.Trim();
        KeyHash = keyHash;
        KeyPrefix = keyPrefix;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
        IsRevoked = false;
    }

    public static (ApiKey ApiKey, string RawKey) Create(
        Guid userId, Guid profileId, string name, DateTimeOffset? expiresAt = null)
    {
        var rawKey = $"pn_{Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant()}";
        var keyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey))).ToLowerInvariant();
        var keyPrefix = rawKey[..11]; // "pn_" + first 8 hex chars

        var apiKey = new ApiKey(Guid.NewGuid(), userId, profileId, name, keyHash, keyPrefix, expiresAt);
        return (apiKey, rawKey);
    }

    public void RecordUsage() => LastUsedAt = DateTimeOffset.UtcNow;
    public void Revoke() => IsRevoked = true;
    public void Rename(string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName, nameof(newName));
        Name = newName.Trim();
    }

    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTimeOffset.UtcNow;
    public bool IsValid => !IsRevoked && !IsExpired;
}
