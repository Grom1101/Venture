using Venture.Application.Abstractions.Caching;

namespace Venture.Application.Caching;

public interface ICacheInvalidator
{
    Task InvalidateUserProfileAsync(Guid userId, string username, CancellationToken cancellationToken = default);

    Task InvalidateUserFeedAsync(Guid userId, CancellationToken cancellationToken = default);
}

internal sealed class CacheInvalidator(ICacheService cache) : ICacheInvalidator
{
    public Task InvalidateUserProfileAsync(
        Guid userId,
        string username,
        CancellationToken cancellationToken = default) =>
        Task.WhenAll(
            cache.RemoveAsync(CacheKeys.UserProfileById(userId), cancellationToken),
            cache.RemoveAsync(CacheKeys.UserProfileByUsername(username), cancellationToken));

    /// <summary>
    /// Атомарный INCR версии ленты (ключ без TTL). Старые page-ключи умрут по своему TTL.
    /// </summary>
    public Task InvalidateUserFeedAsync(Guid userId, CancellationToken cancellationToken = default) =>
        cache.IncrementAsync(CacheKeys.UserFeedVersion(userId), cancellationToken);
}
