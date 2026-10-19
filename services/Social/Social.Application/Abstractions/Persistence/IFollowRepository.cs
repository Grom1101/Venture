using Venture.Domain.Social.Follows;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Abstractions.Persistence;

public interface IFollowRepository
{
    Task<Follow?> GetAsync(UserId followerId, UserId followingId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(UserId followerId, UserId followingId, CancellationToken cancellationToken);

    Task<int> CountFollowersAsync(UserId userId, CancellationToken cancellationToken);

    void Add(Follow follow);

    void Remove(Follow follow);
}
