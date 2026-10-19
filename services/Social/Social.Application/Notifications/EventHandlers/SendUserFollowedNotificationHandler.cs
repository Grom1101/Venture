using Venture.Application.Abstractions.Events;
using Venture.Application.Abstractions.Notifications;
using Venture.Application.Abstractions.Time;
using Venture.Domain.Social.Follows.Events;

namespace Venture.Application.Notifications.EventHandlers;

internal sealed class SendUserFollowedNotificationHandler(
    INotificationWriter notificationWriter,
    IDateTimeProvider dateTimeProvider)
    : IDomainEventHandler<UserFollowedDomainEvent>
{
    public Task Handle(
        DomainEventNotification<UserFollowedDomainEvent> notification,
        CancellationToken cancellationToken) =>
        notificationWriter.AddAsync(
            notification.DomainEvent.FollowingId,
            NotificationType.UserFollowed,
            notification.DomainEvent.FollowerId,
            referenceId: null,
            dateTimeProvider.UtcNow,
            cancellationToken);
}
