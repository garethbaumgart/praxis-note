using PraxisNote.Domain.Common;

namespace PraxisNote.Domain.ValueObjects;

/// <summary>
/// References an action item within a meeting, linking a task to its source.
/// </summary>
/// <remarks>
/// This is a value object that represents the relationship between a Task and
/// the action item in a Meeting that created it. When a task is created from a meeting's
/// action item, this reference enables bidirectional sync.
///
/// Using a primary constructor record here because:
/// - No transformation/validation needed on the values
/// - Simple data carrier with value semantics
/// - Concise single-line definition
/// </remarks>
/// <param name="MeetingId">The ID of the meeting containing the action item.</param>
/// <param name="ActionItemId">The unique identifier of the action item within the meeting.</param>
public sealed record ActionItemRef(Guid MeetingId, Guid ActionItemId) : ValueObject
{
    /// <summary>
    /// Returns true if this reference is valid (has a non-empty meeting ID and action item ID).
    /// </summary>
    public bool IsLinked => MeetingId != Guid.Empty && ActionItemId != Guid.Empty;

    public override string ToString() => $"{MeetingId}:{ActionItemId}";
}
