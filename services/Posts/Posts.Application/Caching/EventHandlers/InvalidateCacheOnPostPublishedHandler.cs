using Venture.Application.Abstractions.Events;
using Venture.Application.Caching;
using Venture.Domain.Posts.Events;

namespace Venture.Application.Caching.EventHandlers;

/// <summary>
/// Новый пост сразу должен появиться в ленте автора; ленты подписчиков догонят по короткому TTL.
/// </summary>
internal sealed class InvalidateCacheOnPostPublishedHandler(ICacheInvalidator cacheInvalidator)
    : IDomainEventHandler<PostPublishedDomainEvent>
{
    public Task Handle(
        DomainEventNotification<PostPublishedDomainEvent> notification,
        CancellationToken cancellationToken) =>
        cacheInvalidator.InvalidateUserFeedAsync(
            notification.DomainEvent.AuthorId.Value,
            cancellationToken);
}
