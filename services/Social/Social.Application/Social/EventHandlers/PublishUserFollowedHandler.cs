using Venture.Application.Abstractions.Events;
using Venture.Application.Abstractions.Integration;
using Venture.Application.Integration;
using Venture.Domain.Social.Follows.Events;

namespace Venture.Application.Social.EventHandlers;

internal sealed class PublishUserFollowedHandler(
    IPostCommitActions postCommitActions,
    IFollowFacts followFacts)
    : IDomainEventHandler<UserFollowedDomainEvent>
{
    public Task Handle(
        DomainEventNotification<UserFollowedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var followerId = notification.DomainEvent.FollowerId.Value;
        var followingId = notification.DomainEvent.FollowingId.Value;

        postCommitActions.Enqueue(token =>
            followFacts.PublishUserFollowedAsync(followerId, followingId, token));

        return Task.CompletedTask;
    }
}
