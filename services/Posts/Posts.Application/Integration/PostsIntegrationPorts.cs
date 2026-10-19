namespace Venture.Application.Integration;

public interface IFollowedUsers
{
    Task<IReadOnlyCollection<Guid>> GetFollowingIdsAsync(Guid followerId, CancellationToken cancellationToken);
}

public interface IUserCards
{
    Task<IReadOnlyDictionary<Guid, UserCard>> GetAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);
}

public sealed record UserCard(Guid Id, string Username, string DisplayName);

public interface IPostFacts
{
    Task PublishPostPublishedAsync(Guid postId, Guid authorId, CancellationToken cancellationToken);
}
