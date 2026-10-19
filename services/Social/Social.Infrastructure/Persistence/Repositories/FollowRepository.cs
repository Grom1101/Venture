using Microsoft.EntityFrameworkCore;
using Venture.Application.Abstractions.Persistence;
using Venture.Domain.Social.Follows;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Infrastructure.Persistence.Repositories;

internal sealed class FollowRepository(VentureDbContext dbContext) : IFollowRepository
{
    public Task<Follow?> GetAsync(UserId followerId, UserId followingId, CancellationToken cancellationToken) =>
        dbContext.Follows.FirstOrDefaultAsync(
            follow => follow.FollowerId == followerId && follow.FollowingId == followingId,
            cancellationToken);

    public Task<bool> ExistsAsync(UserId followerId, UserId followingId, CancellationToken cancellationToken) =>
        dbContext.Follows.AnyAsync(
            follow => follow.FollowerId == followerId && follow.FollowingId == followingId,
            cancellationToken);

    public Task<int> CountFollowersAsync(UserId userId, CancellationToken cancellationToken) =>
        dbContext.Follows.CountAsync(follow => follow.FollowingId == userId, cancellationToken);

    public void Add(Follow follow) => dbContext.Follows.Add(follow);

    public void Remove(Follow follow) => dbContext.Follows.Remove(follow);
}
