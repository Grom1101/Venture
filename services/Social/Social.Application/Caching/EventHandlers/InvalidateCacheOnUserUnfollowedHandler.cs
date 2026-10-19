using Venture.Application.Abstractions.Events;
using Venture.Application.Caching;
using Venture.Application.Integration;
using Venture.Domain.Social.Follows.Events;

namespace Venture.Application.Caching.EventHandlers;

internal sealed class InvalidateCacheOnUserUnfollowedHandler(
    IUserProfiles userProfiles,
    ICacheInvalidator cacheInvalidator)
    : IDomainEventHandler<UserUnfollowedDomainEvent>
{
    public async Task Handle(
        DomainEventNotification<UserUnfollowedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var followerId = notification.DomainEvent.FollowerId;
        var followingId = notification.DomainEvent.FollowingId;

        await cacheInvalidator.InvalidateUserFeedAsync(followerId.Value, cancellationToken);

        var follower = await userProfiles.FindAsync(followerId.Value, cancellationToken);
        if (follower is not null)
            await cacheInvalidator.InvalidateUserProfileAsync(follower.Id, follower.Username, cancellationToken);

        var following = await userProfiles.FindAsync(followingId.Value, cancellationToken);
        if (following is not null)
            await cacheInvalidator.InvalidateUserProfileAsync(following.Id, following.Username, cancellationToken);
    }
}
