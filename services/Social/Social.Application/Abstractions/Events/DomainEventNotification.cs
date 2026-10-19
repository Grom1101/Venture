using MediatR;
using Venture.Domain.Abstractions.Events;

namespace Venture.Application.Abstractions.Events;

/// <summary>
/// Обёртка, благодаря которой домен не знает про MediatR: событие остаётся обычным
/// доменным типом, а в шину его заворачивает Application.
/// </summary>
public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;
