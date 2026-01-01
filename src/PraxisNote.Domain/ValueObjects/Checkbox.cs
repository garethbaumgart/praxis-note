using PraxisNote.Domain.Common;

namespace PraxisNote.Domain.ValueObjects;

/// <summary>
/// Represents a checkbox within a note that can be linked to a task.
/// </summary>
/// <remarks>
/// Key design decisions:
/// - Id is a string (not Guid) to allow editor plugins to generate IDs in their preferred format
/// - Matches CheckboxRef.CheckboxId which is also a string
/// - Text is trimmed but can contain any content
/// - IsChecked maps to task status (checked = Done, unchecked = Todo)
/// </remarks>
public sealed record Checkbox : ValueObject
{
    /// <summary>
    /// Unique identifier within the note. Format depends on the editor plugin.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// The checkbox label/text that becomes the task title.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Whether the checkbox is checked. Maps to task status.
    /// </summary>
    public bool IsChecked { get; }

    public Checkbox(string id, string text, bool isChecked)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        Id = id.Trim();
        Text = text.Trim();
        IsChecked = isChecked;
    }

    /// <summary>
    /// Creates a new Checkbox with updated text.
    /// </summary>
    public Checkbox WithText(string newText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newText);
        return new Checkbox(Id, newText, IsChecked);
    }

    /// <summary>
    /// Creates a new Checkbox with updated checked state.
    /// </summary>
    public Checkbox WithChecked(bool isChecked) => new(Id, Text, isChecked);

    public override string ToString() => $"[{(IsChecked ? "x" : " ")}] {Text}";
}
