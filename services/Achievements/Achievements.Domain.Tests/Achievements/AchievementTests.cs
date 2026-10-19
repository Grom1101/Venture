using FluentAssertions;
using Venture.Domain.Achievements;
using Venture.Domain.Achievements.Enums;
using Venture.Domain.Tests.TestData;

namespace Venture.Domain.Tests.Achievements;

public class AchievementTests
{
    [Fact]
    public void Create_ShouldCreateAchievement()
    {
        var achievement = Achievement.Create(
            AchievementType.FirstSteps,
            "👋 Первопроходец",
            "Зарегистрироваться на сайте",
            DomainFactory.Now).Value;

        achievement.AchievementType.Should().Be(AchievementType.FirstSteps);
        achievement.CreatedOnUtc.Should().Be(DomainFactory.Now);
        achievement.LastModifiedOnUtc.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldFail_WhenTypeIsUnknown()
    {
        var result = Achievement.Create(AchievementType.Unknown, "Название", "Описание", DomainFactory.Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Description.Should().Be("тип достижения должен быть задан");
    }

    [Fact]
    public void Create_ShouldFail_WhenNameIsTooShort()
    {
        var result = Achievement.Create(AchievementType.FirstSteps, "a", "Описание", DomainFactory.Now);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ChangeName_ShouldUpdateNameAndTimestamp()
    {
        var achievement = Achievement.Create(
            AchievementType.FirstSteps,
            "Первый пост",
            "Описание",
            DomainFactory.Now).Value;

        var modifiedAt = DomainFactory.Now.AddDays(1);

        achievement.ChangeName("Дебют", modifiedAt);

        achievement.Name.Should().Be("Дебют");
        achievement.LastModifiedOnUtc.Should().Be(modifiedAt);
    }
}
