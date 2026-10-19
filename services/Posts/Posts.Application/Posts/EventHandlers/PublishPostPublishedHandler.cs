using Venture.Application.Abstractions.Events;
using Venture.Application.Abstractions.Integration;
using Venture.Application.Integration;
using Venture.Domain.Posts.Events;

namespace Venture.Application.Posts.EventHandlers;

internal sealed class PublishPostPublishedHandler(
    IPostCommitActions postCommitActions,
    IPostFacts postFacts)
    : IDomainEventHandler<PostPublishedDomainEvent>
{
    public Task Handle(
        DomainEventNotification<PostPublishedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var postId = notification.DomainEvent.PostId.Value;
        var authorId = notification.DomainEvent.AuthorId.Value;

        postCommitActions.Enqueue(token =>
            postFacts.PublishPostPublishedAsync(postId, authorId, token));

        return Task.CompletedTask;
    }
}
