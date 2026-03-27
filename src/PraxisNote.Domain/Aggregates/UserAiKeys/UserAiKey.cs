using PraxisNote.Domain.Common;

namespace PraxisNote.Domain.Aggregates.UserAiKeys;

public sealed class UserAiKey : AggregateRoot
{
    public Guid UserId { get; private init; }
    public AiProvider Provider { get; private init; }
    public string EncryptedKey { get; private set; } = string.Empty;
    public string KeyHint { get; private set; } = string.Empty;
    public string? PreferredModel { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private UserAiKey() { } // EF Core

    private UserAiKey(Guid id, Guid userId, AiProvider provider, string encryptedKey,
        string keyHint, string? preferredModel) : base(id)
    {
        UserId = userId;
        Provider = provider;
        EncryptedKey = encryptedKey;
        KeyHint = keyHint;
        PreferredModel = string.IsNullOrWhiteSpace(preferredModel) ? null : preferredModel;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static UserAiKey Create(Guid userId, AiProvider provider, string encryptedKey, string keyHint, string? preferredModel)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty, nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedKey, nameof(encryptedKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(keyHint, nameof(keyHint));

        return new UserAiKey(Guid.NewGuid(), userId, provider, encryptedKey, keyHint, preferredModel);
    }

    public void UpdateKey(string encryptedKey, string keyHint, string? preferredModel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedKey, nameof(encryptedKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(keyHint, nameof(keyHint));

        EncryptedKey = encryptedKey;
        KeyHint = keyHint;
        PreferredModel = string.IsNullOrWhiteSpace(preferredModel) ? null : preferredModel;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateModel(string? preferredModel)
    {
        PreferredModel = string.IsNullOrWhiteSpace(preferredModel) ? null : preferredModel;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
