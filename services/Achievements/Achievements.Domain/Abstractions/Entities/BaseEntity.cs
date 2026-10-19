namespace Venture.Domain.Abstractions.Entities;

public abstract class BaseEntity<TId> where TId : notnull
{
    public TId Id { get; protected init; } = default!;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
            return true;

        if (obj is not BaseEntity<TId> other)
            return false;

        if (GetType() != other.GetType())
            return false;

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }
}
