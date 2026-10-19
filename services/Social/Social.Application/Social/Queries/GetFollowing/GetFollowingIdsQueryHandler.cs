using Microsoft.EntityFrameworkCore;
using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Persistence;
using Venture.Domain.Common.Results;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Social.Queries.GetFollowing;

public sealed record GetFollowingIdsQuery(Guid FollowerId) : IQuery<IReadOnlyCollection<Guid>>;

internal sealed class GetFollowingIdsQueryHandler(IReadDbContext readDbContext)
    : IQueryHandler<GetFollowingIdsQuery, IReadOnlyCollection<Guid>>
{
    public async Task<Result<IReadOnlyCollection<Guid>>> Handle(
        GetFollowingIdsQuery query,
        CancellationToken cancellationToken)
    {
        var followerId = UserId.From(query.FollowerId);
        if (followerId.IsFailure)
            return Result.Failure<IReadOnlyCollection<Guid>>(followerId.Error);

        var ids = await readDbContext.Follows
            .Where(follow => follow.FollowerId == followerId.Value)
            .Select(follow => follow.FollowingId.Value)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyCollection<Guid>>(ids);
    }
}
