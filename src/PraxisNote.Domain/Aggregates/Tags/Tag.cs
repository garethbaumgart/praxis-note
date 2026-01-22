using System.Text.RegularExpressions;
using PraxisNote.Domain.Common;

namespace PraxisNote.Domain.Aggregates.Tags;

/// <summary>
/// Tag aggregate - represents a shared organizational tag
/// that can be applied to notes and tasks.
/// </summary>
public sealed partial class Tag : AggregateRoot
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
    /// The hex color code for the tag (e.g., "#3b82f6").
    /// </summary>
    public string Color { get; private set; } = string.Empty;

    /// <summary>
    /// When this tag was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>
    /// Required for EF Core (can access private constructors via reflection).
    /// </summary>
    private Tag() { }

    private Tag(Guid id, Guid userId, string name, string color) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty, nameof(userId));
        ValidateName(name);
        ValidateColor(color);

        UserId = userId;
        Name = name;
        Color = color;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Creates a new tag for the specified user.
    /// </summary>
    /// <param name="userId">The user who owns this tag.</param>
    /// <param name="name">The display name. Must be unique per user.</param>
    /// <param name="color">The hex color code (e.g., "#3b82f6").</param>
    /// <returns>A new Tag instance.</returns>
    public static Tag Create(Guid userId, string name, string color)
    {
        return new Tag(Guid.NewGuid(), userId, name, color);
    }

    /// <summary>
    /// Renames this tag.
    /// </summary>
    /// <param name="newName">The new name for the tag.</param>
    public void Rename(string newName)
    {
        ValidateName(newName);
        Name = newName;
    }

    /// <summary>
    /// Changes this tag's color.
    /// </summary>
    /// <param name="newColor">The new hex color code.</param>
    public void Recolor(string newColor)
    {
        ValidateColor(newColor);
        Color = newColor;
    }

    private static void ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
    }

    private static void ValidateColor(string color)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(color);

        if (!HexColorRegex().IsMatch(color))
        {
            throw new ArgumentException("Color must be a valid hex color code (e.g., #3b82f6)", nameof(color));
        }
    }

    [GeneratedRegex(@"^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColorRegex();
}
