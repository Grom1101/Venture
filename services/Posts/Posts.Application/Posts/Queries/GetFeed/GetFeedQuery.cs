using Venture.Application.Abstractions.Messaging;

namespace Venture.Application.Posts.Queries.GetFeed;

public sealed record GetFeedQuery(int Page = 1, int PageSize = 20) : IQuery<IReadOnlyCollection<FeedItemDto>>;
