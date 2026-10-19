using Venture.Application.Abstractions.Events;
using Venture.Application.Abstractions.Notifications;
using Venture.Application.Abstractions.Time;
using Venture.Domain.Users.Events;

namespace Venture.Application.Notifications.EventHandlers;

internal sealed class SendWelcomeNotificationHandler(
    INotificationWriter notificationWriter,
    IDateTimeProvider dateTimeProvider)
    : IDomainEventHandler<UserRegisteredDomainEvent>
{
    public Task Handle(
        DomainEventNotification<UserRegisteredDomainEvent> notification,
        CancellationToken cancellationToken) =>
        notificationWriter.AddAsync(
            notification.DomainEvent.UserId,
            NotificationType.Welcome,
            actorId: null,
            referenceId: null,
            dateTimeProvider.UtcNow,
            cancellationToken);
}
