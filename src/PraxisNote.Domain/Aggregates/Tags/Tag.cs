using PraxisNote.Domain.Common;

namespace PraxisNote.Domain.Aggregates.Tags;

/// <summary>
/// Tag aggregate - represents a shared organizational tag
/// that can be applied to notes and tasks.
/// </summary>
public sealed class Tag : AggregateRoot
{
    /// <summary>
    /// The user who owns this tag.
    /// </summary>
    public Guid UserId { get; private init; }

    /// <summary>
    /// The display name of the tag. Must be unique per user.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// When this tag was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>
    /// Required for EF Core (can access private constructors via reflection).
    /// </summary>
    private Tag() { }

    private Tag(Guid id, Guid userId, string name) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty, nameof(userId));
        ValidateName(name);

        UserId = userId;
        Name = name.ToLowerInvariant();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Creates a new tag for the specified user.
    /// </summary>
    /// <param name="userId">The user who owns this tag.</param>
    /// <param name="name">The display name. Must be unique per user.</param>
    /// <returns>A new Tag instance.</returns>
    public static Tag Create(Guid userId, string name)
    {
        return new Tag(Guid.NewGuid(), userId, name);
    }

    /// <summary>
    /// Renames this tag.
    /// </summary>
    /// <param name="newName">The new name for the tag.</param>
    public void Rename(string newName)
    {
        ValidateName(newName);
        Name = newName.ToLowerInvariant();
    }

    private static void ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
    }
}
