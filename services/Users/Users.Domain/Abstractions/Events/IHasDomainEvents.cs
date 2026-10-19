namespace Venture.Domain.Abstractions.Events;

/// <summary>
/// Позволяет инфраструктуре собирать доменные события, не зная тип идентификатора агрегата.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    IReadOnlyCollection<IDomainEvent> ClearDomainEvents();
}
