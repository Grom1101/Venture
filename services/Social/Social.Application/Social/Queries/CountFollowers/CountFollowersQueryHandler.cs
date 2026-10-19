using Microsoft.EntityFrameworkCore;
using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Persistence;
using Venture.Domain.Common.Results;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Social.Queries.CountFollowers;

public sealed record CountFollowersQuery(Guid UserId) : IQuery<int>;

internal sealed class CountFollowersQueryHandler(IReadDbContext readDbContext)
    : IQueryHandler<CountFollowersQuery, int>
{
    public async Task<Result<int>> Handle(CountFollowersQuery query, CancellationToken cancellationToken)
    {
        var userId = UserId.From(query.UserId);
        if (userId.IsFailure)
            return Result.Failure<int>(userId.Error);

        var count = await readDbContext.Follows.CountAsync(
            follow => follow.FollowingId == userId.Value,
            cancellationToken);

        return Result.Success(count);
    }
}
