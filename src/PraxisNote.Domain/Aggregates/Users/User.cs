using PraxisNote.Domain.Common;
using PraxisNote.Domain.ValueObjects;

namespace PraxisNote.Domain.Aggregates.Users;

/// <summary>
/// User aggregate - represents an authenticated user who owns
/// all notes, tasks, and labels in the system.
/// </summary>
public sealed class User : AggregateRoot
{
    /// <summary>
    /// The external OAuth provider identity. Immutable after creation.
    /// </summary>
    public ExternalIdentity ExternalIdentity { get; private init; } = null!;

    /// <summary>
    /// The user's email address.
    /// </summary>
    public Email Email { get; private init; } = null!;

    /// <summary>
    /// The user's display name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// URL to the user's avatar image from the external OAuth provider. May be null.
    /// </summary>
    public string? AvatarUrl { get; private set; }

    /// <summary>
    /// When this user account was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>
    /// When the user last logged in.
    /// </summary>
    public DateTimeOffset LastLoginAt { get; private set; }

    /// <summary>
    /// The ID of the last notification the user has seen. Null if no notifications have been seen.
    /// </summary>
    public int? LastSeenNotificationId { get; private set; }

    /// <summary>
    /// External identities linked to this user account (e.g., additional Google accounts).
    /// </summary>
    private readonly List<LinkedIdentity> _linkedIdentities = [];
    public IReadOnlyCollection<LinkedIdentity> LinkedIdentities => _linkedIdentities.AsReadOnly();

    /// <summary>
    /// Required for EF Core.
    /// </summary>
    private User() { }

    private User(Guid id, ExternalIdentity externalIdentity, Email email, string name, string? avatarUrl) : base(id)
    {
        ArgumentNullException.ThrowIfNull(externalIdentity);
        ArgumentNullException.ThrowIfNull(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var now = DateTimeOffset.UtcNow;

        ExternalIdentity = externalIdentity;
        Email = email;
        Name = name;
        AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl;
        CreatedAt = now;
        LastLoginAt = now;
    }

    /// <summary>
    /// Registers a new user from an external OAuth provider.
    /// </summary>
    /// <param name="externalIdentity">The external OAuth provider identity.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="name">The user's display name.</param>
    /// <param name="avatarUrl">Optional URL to the user's avatar.</param>
    /// <returns>A new User instance.</returns>
    public static User Register(ExternalIdentity externalIdentity, Email email, string name, string? avatarUrl = null)
    {
        return new User(Guid.NewGuid(), externalIdentity, email, name, avatarUrl);
    }

    /// <summary>
    /// Records that the user has logged in, updating the last login timestamp and optionally the avatar.
    /// </summary>
    /// <param name="avatarUrl">The updated avatar URL from the OAuth provider, or null to leave unchanged.</param>
    public void RecordLogin(string? avatarUrl = null)
    {
        LastLoginAt = DateTimeOffset.UtcNow;

        // Update avatar if a new one is provided (allows keeping existing avatar if OAuth doesn't return one)
        if (!string.IsNullOrWhiteSpace(avatarUrl))
        {
            AvatarUrl = avatarUrl;
        }
    }

    /// <summary>
    /// Updates the last seen notification ID. Only updates if the new ID is greater than the current one.
    /// </summary>
    /// <param name="notificationId">The notification ID the user has now seen.</param>
    public void UpdateLastSeenNotificationId(int notificationId)
    {
        if (LastSeenNotificationId is null || notificationId > LastSeenNotificationId)
        {
            LastSeenNotificationId = notificationId;
        }
    }
}
