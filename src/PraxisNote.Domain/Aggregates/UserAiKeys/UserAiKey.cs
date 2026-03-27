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
        var now = DateTimeOffset.UtcNow;
        UserId = userId;
        Provider = provider;
        EncryptedKey = encryptedKey;
        KeyHint = keyHint;
        PreferredModel = string.IsNullOrWhiteSpace(preferredModel) ? null : preferredModel;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public static UserAiKey Create(Guid userId, AiProvider provider, string encryptedKey, string keyHint, string? preferredModel)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty, nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedKey, nameof(encryptedKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(keyHint, nameof(keyHint));
        ValidatePreferredModelLength(preferredModel);

        return new UserAiKey(Guid.NewGuid(), userId, provider, encryptedKey, keyHint, preferredModel);
    }

    public void UpdateKey(string encryptedKey, string keyHint, string? preferredModel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedKey, nameof(encryptedKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(keyHint, nameof(keyHint));
        ValidatePreferredModelLength(preferredModel);

        EncryptedKey = encryptedKey;
        KeyHint = keyHint;
        PreferredModel = string.IsNullOrWhiteSpace(preferredModel) ? null : preferredModel;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateModel(string? preferredModel)
    {
        ValidatePreferredModelLength(preferredModel);
        PreferredModel = string.IsNullOrWhiteSpace(preferredModel) ? null : preferredModel.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidatePreferredModelLength(string? preferredModel)
    {
        if (preferredModel is not null && preferredModel.Length > 100)
            throw new ArgumentException("PreferredModel must be 100 characters or fewer.", nameof(preferredModel));
    }
}
