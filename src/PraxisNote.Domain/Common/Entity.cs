namespace PraxisNote.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// Entities have identity and are compared by their Id, not their properties.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    /// <summary>
    /// Unique identifier for this entity.
    /// </summary>
    public Guid Id { get; protected init; }

    protected Entity(Guid id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty, nameof(id));
        Id = id;
    }

    /// <summary>
    /// Required for EF Core and serialization.
    /// </summary>
    protected Entity() { }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Entity)obj);
    }

    public bool Equals(Entity? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (other.GetType() != GetType()) return false;
        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}
