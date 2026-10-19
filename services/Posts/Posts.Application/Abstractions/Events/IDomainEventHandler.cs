using MediatR;
using Venture.Domain.Abstractions.Events;

namespace Venture.Application.Abstractions.Events;

public interface IDomainEventHandler<TDomainEvent> : INotificationHandler<DomainEventNotification<TDomainEvent>>
    where TDomainEvent : IDomainEvent;
