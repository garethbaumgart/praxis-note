namespace PraxisNote.Domain.Events;

/// <summary>
/// Base record for domain events providing common functionality.
/// </summary>
public abstract record DomainEventBase : IDomainEvent
{
    /// <inheritdoc />
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
