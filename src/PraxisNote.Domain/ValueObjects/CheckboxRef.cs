using PraxisNote.Domain.Common;

namespace PraxisNote.Domain.ValueObjects;

/// <summary>
/// References a checkbox within a note, linking a task to its source.
/// </summary>
/// <remarks>
/// This is a value object that represents the relationship between a Task and
/// the checkbox in a Note that created it. When a task is created from a note's
/// checkbox, this reference enables bidirectional sync.
///
/// Using a primary constructor record here because:
/// - No transformation/validation needed on the values
/// - Simple data carrier with value semantics
/// - Concise single-line definition
/// </remarks>
/// <param name="NoteId">The ID of the note containing the checkbox.</param>
/// <param name="CheckboxId">The unique identifier of the checkbox within the note.</param>
public sealed record CheckboxRef(Guid NoteId, string CheckboxId) : ValueObject
{
    /// <summary>
    /// Returns true if this reference points to a valid note.
    /// </summary>
    public bool IsLinked => NoteId != Guid.Empty && !string.IsNullOrWhiteSpace(CheckboxId);

    public override string ToString() => $"{NoteId}:{CheckboxId}";
}
