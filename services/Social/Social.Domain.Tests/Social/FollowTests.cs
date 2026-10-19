using FluentAssertions;
using Venture.Domain.Social.Follows;
using Venture.Domain.Social.Follows.Events;
using Venture.Domain.Tests.TestData;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Domain.Tests.Social;

public class FollowTests
{
    [Fact]
    public void Create_ShouldFail_WhenFollowingYourself()
    {
        var userId = UserId.New();

        var result = Follow.Create(userId, userId, DomainFactory.Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Description.Should().Be("нельзя подписаться на самого себя");
    }

    [Fact]
    public void Create_ShouldCreateFollow_AndRaiseEvent()
    {
        var followerId = UserId.New();
        var followingId = UserId.New();

        var follow = Follow.Create(followerId, followingId, DomainFactory.Now).Value;

        follow.FollowerId.Should().Be(followerId);
        follow.FollowingId.Should().Be(followingId);
        follow.CreatedAtUtc.Should().Be(DomainFactory.Now);
        follow.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserFollowedDomainEvent>();
    }

    [Fact]
    public void Unfollow_ShouldRaiseUserUnfollowedDomainEvent()
    {
        var follow = Follow.Create(UserId.New(), UserId.New(), DomainFactory.Now).Value;
        follow.ClearDomainEvents();

        follow.Unfollow();

        follow.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserUnfollowedDomainEvent>();
    }
}
