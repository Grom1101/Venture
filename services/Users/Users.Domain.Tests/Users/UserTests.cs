using FluentAssertions;
using Venture.Domain.Tests.TestData;
using Venture.Domain.Users.Enums;
using Venture.Domain.Users.Events;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Domain.Tests.Users;

public class UserTests
{
    [Fact]
    public void Register_ShouldRaiseUserRegisteredDomainEvent()
    {
        var user = DomainFactory.User();

        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserRegisteredDomainEvent>()
            .Which.UserId.Should().Be(user.Id);
    }

    [Fact]
    public void Register_ShouldUseDefaultBio_WhenBioIsNotProvided()
    {
        var user = DomainFactory.User();

        user.Bio.Should().Be(Bio.Default());
        user.Skills.Should().BeEmpty();
    }

    [Fact]
    public void Register_ShouldNormalizeUsername()
    {
        var user = DomainFactory.User(username: "  JohnDoe ");

        user.Username.Value.Should().Be("johndoe");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]
    [InlineData("1johndoe")]
    public void CreateUsername_ShouldFail_WhenValueIsInvalid(string value)
    {
        var result = Username.Create(value);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void CreateUsername_ShouldFail_WhenValueIsTooLong()
    {
        var result = Username.Create(new string('a', Username.MaxLength + 1));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AddSkill_ShouldAddSkill()
    {
        var user = DomainFactory.User();

        user.AddSkill(SkillName.Create("C#").Value, SkillLevel.Advanced);

        user.Skills.Should().ContainSingle()
            .Which.Name.Value.Should().Be("C#");
    }

    [Fact]
    public void AddSkill_ShouldFail_WhenSkillAlreadyExists()
    {
        var user = DomainFactory.User();
        user.AddSkill(SkillName.Create("C#").Value, SkillLevel.Advanced);

        var result = user.AddSkill(SkillName.Create("c#").Value, SkillLevel.Beginner);

        result.IsFailure.Should().BeTrue();
        result.Error.Description.Should().Be("пользователь уже имеет этот навык");
    }

    [Fact]
    public void RemoveSkill_ShouldRemoveSkill()
    {
        var user = DomainFactory.User();
        user.AddSkill(SkillName.Create("C#").Value, SkillLevel.Advanced);

        user.RemoveSkill(SkillName.Create("C#").Value);

        user.Skills.Should().BeEmpty();
    }

    [Fact]
    public void RemoveSkill_ShouldFail_WhenSkillIsMissing()
    {
        var user = DomainFactory.User();

        var result = user.RemoveSkill(SkillName.Create("Go").Value);

        result.IsFailure.Should().BeTrue();
        result.Error.Description.Should().Be("навык не найден");
    }

    [Fact]
    public void ChangeProfile_ShouldUpdateBio_AndRaiseEvent()
    {
        var user = DomainFactory.User();
        user.ClearDomainEvents();

        var bio = Bio.Create(
            "Строю Venture",
            "DDD и Clean Architecture",
            "без комментариев",
            [Interest.Create("DDD").Value]).Value;

        user.ChangeProfile(DisplayName.Create("Джон").Value, bio);

        user.DisplayName.Value.Should().Be("Джон");
        user.Bio.Should().Be(bio);
        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserProfileChangedDomainEvent>();
    }

    [Fact]
    public void ChangeProfile_ShouldNotRaiseEvent_WhenNothingChanged()
    {
        var user = DomainFactory.User();
        user.ClearDomainEvents();

        user.ChangeProfile(user.DisplayName, user.Bio);

        user.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void CreateBio_ShouldFail_WhenAboutIsTooLong()
    {
        var result = Bio.Create(new string('a', 101), "focus", "comment", []);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AddInterest_ShouldReturnNewBio_LeavingOriginalUnchanged()
    {
        var bio = Bio.Default();

        var updated = bio.AddInterest(Interest.Create("DDD").Value);

        bio.Interests.Should().BeEmpty();
        updated.Value.Interests.Should().ContainSingle();
    }
}
