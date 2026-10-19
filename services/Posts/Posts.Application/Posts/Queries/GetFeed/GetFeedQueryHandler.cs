using Microsoft.EntityFrameworkCore;
using Venture.Application.Abstractions.Authentication;
using Venture.Application.Abstractions.Caching;
using Venture.Application.Abstractions.Integration;
using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Persistence;
using Venture.Application.Caching;
using Venture.Domain.Common.Results;
using Venture.Application.Integration;
using Venture.Application.Users;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Posts.Queries.GetFeed;

/// <summary>
/// Лента — read-side постов этого сервиса. Авторы и подписки читаются из своих сервисов,
/// лайки остаются в базе постов.
/// </summary>
internal sealed class GetFeedQueryHandler(
    IReadDbContext readDbContext,
    ICurrentUser currentUser,
    IFollowedUsers followedUsers,
    IUserCards userCards,
    ICacheService cache)
    : IQueryHandler<GetFeedQuery, IReadOnlyCollection<FeedItemDto>>
{
    public async Task<Result<IReadOnlyCollection<FeedItemDto>>> Handle(
        GetFeedQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } currentUserId)
            return Result.Failure<IReadOnlyCollection<FeedItemDto>>(UserErrors.NotAuthenticated);

        var feedVersion = await cache.GetCounterAsync(
            CacheKeys.UserFeedVersion(currentUserId.Value),
            cancellationToken);

        var cacheKey = CacheKeys.UserFeed(currentUserId.Value, feedVersion, query.Page, query.PageSize);
        var cached = await cache.GetAsync<FeedItemDto[]>(cacheKey, cancellationToken);

        if (cached is not null)
            return Result.Success<IReadOnlyCollection<FeedItemDto>>(cached);

        var followingIds = await followedUsers.GetFollowingIdsAsync(currentUserId.Value, cancellationToken);
        var parsedAuthorIds = followingIds
            .Append(currentUserId.Value)
            .Distinct()
            .Select(UserId.From)
            .ToArray();
        var invalidAuthorId = parsedAuthorIds.FirstOrDefault(authorId => authorId.IsFailure);
        if (invalidAuthorId is not null)
            return Result.Failure<IReadOnlyCollection<FeedItemDto>>(invalidAuthorId.Error);

        var authorIds = parsedAuthorIds.Select(authorId => authorId.Value).ToArray();

        var rows = await readDbContext.Posts
            .Where(post => !post.IsDeleted)
            .Where(post => authorIds.Contains(post.AuthorId))
            .OrderByDescending(post => post.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(post => new
            {
                post.Id,
                post.Content,
                post.CreatedAtUtc,
                post.AuthorId,
                LikesCount = readDbContext.PostLikes.Count(like => like.PostId == post.Id),
                LikedByMe = readDbContext.PostLikes
                    .Any(like => like.PostId == post.Id && like.UserId == currentUserId)
            })
            .ToListAsync(cancellationToken);

        var authors = await userCards.GetAsync(
            rows.Select(row => row.AuthorId.Value).Distinct().ToArray(),
            cancellationToken);

        var feed = rows
            .Where(row => authors.ContainsKey(row.AuthorId.Value))
            .Select(row =>
            {
                var author = authors[row.AuthorId.Value];
                return new FeedItemDto(
                    row.Id.Value,
                    author.Id,
                    author.Username,
                    author.DisplayName,
                    row.Content.Value,
                    row.LikesCount,
                    row.LikedByMe,
                    row.CreatedAtUtc);
            })
            .ToArray();

        await cache.SetAsync(cacheKey, feed, CacheDurations.UserFeed, cancellationToken);

        return Result.Success<IReadOnlyCollection<FeedItemDto>>(feed);
    }
}
