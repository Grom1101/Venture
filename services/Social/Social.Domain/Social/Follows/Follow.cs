using Venture.Domain.Abstractions.Aggregates;
using Venture.Domain.Common.Results;
using Venture.Domain.Social.Follows.Events;
using Venture.Domain.Social.Follows.ValueObjects;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Domain.Social.Follows;

/// <summary>
/// Подписка — отдельный агрегат, чтобы список подписчиков не раздувал агрегат User.
/// Уникальность пары (follower, following) дополнительно защищена unique-индексом в БД.
/// </summary>
public class Follow : AggregateRoot<FollowId>
{
    public UserId FollowerId { get; private set; }

    public UserId FollowingId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private Follow()
    {
        FollowerId = null!;
        FollowingId = null!;
    }

    private Follow(FollowId id, UserId followerId, UserId followingId, DateTime createdAtUtc)
    {
        Id = id;
        FollowerId = followerId;
        FollowingId = followingId;
        CreatedAtUtc = createdAtUtc;
    }

    public static Result<Follow> Create(UserId followerId, UserId followingId, DateTime createdAtUtc)
    {
        if (followerId is null || followingId is null)
            return Result.Failure<Follow>(Error.Conflict(
                "Social.UserId.Empty",
                "StronglyTypedId cannot be an empty guid."));

        if (followerId == followingId)
            return Result.Failure<Follow>(Error.Conflict(
                "Social.CannotFollowYourself",
                "нельзя подписаться на самого себя"));

        var follow = new Follow(FollowId.New(), followerId, followingId, createdAtUtc);

        follow.AddDomainEvent(new UserFollowedDomainEvent(followerId, followingId));

        return Result.Success(follow);
    }

    public void Unfollow()
    {
        AddDomainEvent(new UserUnfollowedDomainEvent(FollowerId, FollowingId));
    }
}
