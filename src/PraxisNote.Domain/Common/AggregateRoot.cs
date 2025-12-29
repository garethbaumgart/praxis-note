using PraxisNote.Domain.Events;

namespace PraxisNote.Domain.Common;

/// <summary>
/// Base class for aggregate roots.
/// Aggregate roots are the entry point to an aggregate and are responsible for
/// maintaining invariants across the aggregate boundary.
/// They also track domain events that occur within the aggregate.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Domain events raised by this aggregate, pending dispatch.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot(Guid id) : base(id) { }

    /// <summary>
    /// Required for EF Core and serialization.
    /// </summary>
    protected AggregateRoot() { }

    /// <summary>
    /// Raises a domain event to be dispatched after the aggregate is persisted.
    /// </summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events. Called by infrastructure after events are dispatched.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
