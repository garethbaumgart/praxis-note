namespace PraxisNote.Domain.Events;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something significant that happened in the domain.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// When the event occurred.
    /// </summary>
    DateTimeOffset OccurredAt { get; }
}
