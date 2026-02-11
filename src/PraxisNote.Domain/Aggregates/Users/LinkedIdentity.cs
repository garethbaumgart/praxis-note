using PraxisNote.Domain.Common;

namespace PraxisNote.Domain.Aggregates.Users;

/// <summary>
/// Represents an external OAuth identity linked to a user account.
/// Multiple identities can be linked to one user, allowing login from different Google accounts.
/// </summary>
public sealed class LinkedIdentity : Entity
{
    /// <summary>
    /// The user this identity is linked to.
    /// </summary>
    public Guid UserId { get; private init; }

    /// <summary>
    /// The OAuth provider name (e.g., "google").
    /// </summary>
    public string Provider { get; private init; } = string.Empty;

    /// <summary>
    /// The unique identifier from the OAuth provider.
    /// </summary>
    public string ProviderId { get; private init; } = string.Empty;

    /// <summary>
    /// The email address associated with this identity.
    /// </summary>
    public string Email { get; private init; } = string.Empty;

    /// <summary>
    /// The display name associated with this identity.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Optional avatar URL from the OAuth provider.
    /// </summary>
    public string? AvatarUrl { get; private set; }

    /// <summary>
    /// The default profile to activate when logging in with this identity. Null means use user's default profile.
    /// </summary>
    public Guid? DefaultProfileId { get; private set; }

    /// <summary>
    /// When this identity was linked to the user account.
    /// </summary>
    public DateTimeOffset LinkedAt { get; private init; }

    /// <summary>
    /// Required for EF Core.
    /// </summary>
    private LinkedIdentity() { }

    private LinkedIdentity(Guid id) : base(id) { }

    /// <summary>
    /// Creates a new linked identity for the specified user.
    /// </summary>
    public static LinkedIdentity Create(
        Guid userId,
        string provider,
        string providerId,
        string email,
        string name,
        string? avatarUrl = null,
        Guid? defaultProfileId = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty, nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new LinkedIdentity(Guid.NewGuid())
        {
            UserId = userId,
            Provider = provider.Trim().ToLowerInvariant(),
            ProviderId = providerId.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl,
            DefaultProfileId = defaultProfileId,
            LinkedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Sets the default profile to activate when logging in with this identity.
    /// </summary>
    public void SetDefaultProfile(Guid? profileId)
    {
        DefaultProfileId = profileId;
    }
}
