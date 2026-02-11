using PraxisNote.Domain.Common;

namespace PraxisNote.Domain.Aggregates.Profiles;

/// <summary>
/// Profile aggregate - represents a data silo within a user account.
/// Each profile contains its own set of tasks, notes, meetings, tags, goals, and calendar connections.
/// Users who never create a second profile see no change — the default profile works transparently.
/// </summary>
/// <remarks>
/// Key design decisions:
/// - UserId is the security boundary; ProfileId is the data silo boundary
/// - A user can have up to 5 profiles
/// - One profile must always be marked as default
/// - Profile deletion requires moving data to another profile first
/// </remarks>
public sealed class Profile : AggregateRoot
{
    /// <summary>
    /// The user who owns this profile.
    /// </summary>
    public Guid UserId { get; private init; }

    /// <summary>
    /// The display name of the profile.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Optional icon identifier for the profile.
    /// </summary>
    public string? Icon { get; private set; }

    /// <summary>
    /// Whether this is the user's default profile.
    /// </summary>
    public bool IsDefault { get; private set; }

    /// <summary>
    /// When this profile was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>
    /// When this profile was last modified.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Required for EF Core.
    /// </summary>
    private Profile() { }

    private Profile(Guid id, Guid userId, string name, string? icon, bool isDefault) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty, nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var now = DateTimeOffset.UtcNow;

        UserId = userId;
        Name = name.Trim();
        Icon = icon;
        IsDefault = isDefault;
        CreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Creates a new profile for the specified user.
    /// </summary>
    public static Profile Create(Guid userId, string name, string? icon = null, bool isDefault = false)
    {
        return new Profile(Guid.NewGuid(), userId, name, icon, isDefault);
    }

    /// <summary>
    /// Creates a default profile for a new user.
    /// </summary>
    public static Profile CreateDefault(Guid userId)
    {
        return Create(userId, "Default", icon: null, isDefault: true);
    }

    /// <summary>
    /// Renames this profile.
    /// </summary>
    public void Rename(string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);

        Name = newName.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the profile icon.
    /// </summary>
    public void SetIcon(string? icon)
    {
        Icon = icon;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks this profile as the user's default.
    /// </summary>
    public void SetAsDefault()
    {
        IsDefault = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Clears the default flag on this profile.
    /// </summary>
    public void ClearDefault()
    {
        IsDefault = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
