using Venture.Domain.Abstractions.Entities;
using Venture.Domain.Abstractions.Events;
using Venture.Domain.Common.Results;

namespace Venture.Domain.Abstractions.Aggregates;

public abstract class AggregateRoot<TId> : BaseEntity<TId>, IHasDomainEvents where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected Result AddDomainEvent(IDomainEvent domainEvent)
    {
        if (domainEvent is null)
            return Result.Failure(Error.Conflict("Domain.Event.Missing", "доменное событие не задано"));

        _domainEvents.Add(domainEvent);
        return Result.Success();
    }

    public IReadOnlyCollection<IDomainEvent> ClearDomainEvents()
    {
        var events = _domainEvents.ToArray();

        _domainEvents.Clear();

        return events;
    }
}