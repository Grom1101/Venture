using Venture.Application.Abstractions.Events;
using Venture.Application.Caching;
using Venture.Domain.Users.Events;

namespace Venture.Application.Caching.EventHandlers;

internal sealed class InvalidateCacheOnUserProfileChangedHandler(
    ICacheInvalidator cacheInvalidator)
    : IDomainEventHandler<UserProfileChangedDomainEvent>
{
    public Task Handle(
        DomainEventNotification<UserProfileChangedDomainEvent> notification,
        CancellationToken cancellationToken) =>
        cacheInvalidator.InvalidateUserProfileAsync(
            notification.DomainEvent.UserId.Value,
            notification.DomainEvent.Username.Value,
            cancellationToken);
}
