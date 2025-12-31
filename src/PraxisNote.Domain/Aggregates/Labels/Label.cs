using PraxisNote.Domain.Common;

namespace PraxisNote.Domain.Aggregates.Labels;

/// <summary>
/// Label aggregate - represents a shared organizational tag
/// that can be applied to notes and tasks.
/// </summary>
public sealed class Label : AggregateRoot
{
    /// <summary>
    /// The user who owns this label.
    /// </summary>
    public Guid UserId { get; private init; }

    /// <summary>
    /// The display name of the label. Must be unique per user.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// When this label was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>
    /// Required for EF Core (can access private constructors via reflection).
    /// </summary>
    private Label() { }

    private Label(Guid id, Guid userId, string name) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty, nameof(userId));
        ValidateName(name);

        UserId = userId;
        Name = name;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Creates a new label for the specified user.
    /// </summary>
    /// <param name="userId">The user who owns this label.</param>
    /// <param name="name">The display name. Must be unique per user.</param>
    /// <returns>A new Label instance.</returns>
    public static Label Create(Guid userId, string name)
    {
        return new Label(Guid.NewGuid(), userId, name);
    }

    /// <summary>
    /// Renames this label.
    /// </summary>
    /// <param name="newName">The new name for the label.</param>
    public void Rename(string newName)
    {
        ValidateName(newName);
        Name = newName;
    }

    private static void ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
    }
}
