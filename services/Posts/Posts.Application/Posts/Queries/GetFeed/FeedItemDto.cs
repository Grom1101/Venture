namespace Venture.Application.Posts.Queries.GetFeed;

public sealed record FeedItemDto(
    Guid PostId,
    Guid AuthorId,
    string AuthorUsername,
    string AuthorDisplayName,
    string Content,
    int LikesCount,
    bool LikedByMe,
    DateTime CreatedAtUtc);
