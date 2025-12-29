namespace PraxisNote.Domain.Common;

/// <summary>
/// Base record for value objects.
/// Value objects are immutable and compared by their values, not identity.
///
/// Records provide value-based equality automatically, so in most cases
/// you can simply inherit from this base:
/// <code>
/// public record Email(string Value) : ValueObject;
/// </code>
/// </summary>
public abstract record ValueObject;
