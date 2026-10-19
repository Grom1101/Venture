using FluentAssertions;
using Venture.Domain.Achievements;
using Venture.Domain.Achievements.ValueObjects;
using Venture.Domain.Tests.TestData;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Domain.Tests.Achievements;

public class UserAchievementProgressTests
{
    private readonly UserId _userId = UserId.New();
    private readonly AchievementId _firstPostId = AchievementId.New();
    private readonly AchievementId _firstFollowerId = AchievementId.New();

    [Fact]
    public void ObservePost_GrantsFirstPost_OnTheFirstAcceptedFact()
    {
        var progress = UserAchievementProgress.Start(_userId).Value;

        progress.ObservePost(_firstPostId, DomainFactory.Now);

        progress.PostsObserved.Should().Be(1);
        progress.FollowersObserved.Should().Be(0);
        progress.Earned.Should().ContainSingle();
        progress.Earned.Single().AchievementId.Should().Be(_firstPostId);
        progress.Earned.Single().EarnedOnUtc.Should().Be(DomainFactory.Now);
    }

    [Fact]
    public void ObservePost_DoesNotGrantAgain_WhenAnotherPostArrives()
    {
        var progress = UserAchievementProgress.Start(_userId).Value;
        progress.ObservePost(_firstPostId, DomainFactory.Now);

        progress.ObservePost(_firstPostId, DomainFactory.Now.AddMinutes(5));

        progress.PostsObserved.Should().Be(2);
        progress.Earned.Should().ContainSingle();
        progress.Earned.Single().EarnedOnUtc.Should().Be(DomainFactory.Now);
    }

    [Fact]
    public void ObserveFollower_DoesNotGrantThePostAchievement()
    {
        var progress = UserAchievementProgress.Start(_userId).Value;

        progress.ObserveFollower(_firstFollowerId, DomainFactory.Now);

        progress.PostsObserved.Should().Be(0);
        progress.FollowersObserved.Should().Be(1);
        progress.Has(_firstPostId).Should().BeFalse();
        progress.Has(_firstFollowerId).Should().BeTrue();
    }
}
