using FluentValidation;
using Venture.Domain.Posts;

namespace Venture.Application.Posts.Queries.GetFeed;

public sealed class GetFeedQueryValidator : AbstractValidator<GetFeedQuery>
{
    public GetFeedQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(FeedPage.MinPage);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(FeedPage.MinPageSize, FeedPage.MaxPageSize);
    }
}
