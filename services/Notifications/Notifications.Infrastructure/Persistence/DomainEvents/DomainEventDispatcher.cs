using System.Collections.Concurrent;
using MediatR;
using Venture.Application.Abstractions.Events;
using Venture.Domain.Abstractions.Events;

namespace Venture.Infrastructure.Persistence.DomainEvents;

internal sealed class DomainEventDispatcher(IPublisher publisher) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, Type> NotificationTypes = new();

    public async Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            var notificationType = NotificationTypes.GetOrAdd(
                domainEvent.GetType(),
                eventType => typeof(DomainEventNotification<>).MakeGenericType(eventType));

            var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;

            await publisher.Publish(notification, cancellationToken);
        }
    }
}
