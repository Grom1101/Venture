using Microsoft.EntityFrameworkCore;
using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Persistence;
using Venture.Domain.Common.Results;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Social.Queries.GetFollowStats;

public sealed record FollowStatsDto(int Followers, int Following);

public sealed record GetFollowStatsQuery(Guid UserId) : IQuery<FollowStatsDto>;

internal sealed class GetFollowStatsQueryHandler(IReadDbContext readDbContext)
    : IQueryHandler<GetFollowStatsQuery, FollowStatsDto>
{
    public async Task<Result<FollowStatsDto>> Handle(GetFollowStatsQuery query, CancellationToken cancellationToken)
    {
        var userId = UserId.From(query.UserId);
        if (userId.IsFailure)
            return Result.Failure<FollowStatsDto>(userId.Error);

        var followers = await readDbContext.Follows.CountAsync(
            follow => follow.FollowingId == userId.Value,
            cancellationToken);

        var following = await readDbContext.Follows.CountAsync(
            follow => follow.FollowerId == userId.Value,
            cancellationToken);

        return Result.Success(new FollowStatsDto(followers, following));
    }
}
