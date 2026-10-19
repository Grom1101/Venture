using FluentAssertions;
using NSubstitute;
using Venture.Application.Abstractions.Authentication;
using Venture.Application.Abstractions.Persistence;
using Venture.Domain.Common.Results;
using Venture.Application.Tests.TestData;
using Venture.Application.Users;
using Venture.Application.Users.Commands.AddSkill;
using Venture.Domain.Users;
using Venture.Domain.Users.Enums;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Tests.Users;

public class AddSkillCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private readonly AddSkillCommandHandler _handler;

    public AddSkillCommandHandlerTests()
    {
        _handler = new AddSkillCommandHandler(_userRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenUserIsNotAuthenticated()
    {
        _currentUser.UserId.Returns((UserId?)null);

        var result = await _handler.Handle(new AddSkillCommand("C#", SkillLevel.Advanced), CancellationToken.None);

        result.Error.Should().Be(UserErrors.NotAuthenticated);
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenUserDoesNotExist()
    {
        var userId = UserId.New();
        _currentUser.UserId.Returns(userId);
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _handler.Handle(new AddSkillCommand("C#", SkillLevel.Advanced), CancellationToken.None);

        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldAddSkillToAggregate()
    {
        var user = ApplicationFactory.User();
        _currentUser.UserId.Returns(user.Id);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new AddSkillCommand("C#", SkillLevel.Advanced), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.Skills.Should().ContainSingle()
            .Which.Name.Value.Should().Be("C#");
    }
}
