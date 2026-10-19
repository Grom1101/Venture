namespace Venture.Application.Integration;

public interface IUserProfiles
{
    Task<UserProfileRef?> FindAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed record UserProfileRef(Guid Id, string Username);

public interface IFollowFacts
{
    Task PublishUserFollowedAsync(Guid followerId, Guid followingId, CancellationToken cancellationToken);
}
