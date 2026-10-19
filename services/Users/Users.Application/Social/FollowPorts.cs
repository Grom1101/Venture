using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Social;

public interface IFollowerCount
{
    Task<int> CountFollowersAsync(UserId userId, CancellationToken cancellationToken);
}

public interface IFollowStats
{
    Task<FollowStats> GetAsync(UserId userId, CancellationToken cancellationToken);
}

public sealed record FollowStats(int Followers, int Following);
