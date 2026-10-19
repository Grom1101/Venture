using FluentAssertions;
using NSubstitute;
using Venture.Application.Abstractions.Authentication;
using Venture.Application.Abstractions.Persistence;
using Venture.Application.Abstractions.Time;
using Venture.Application.Social;
using Venture.Application.Social.Commands.FollowUser;
using Venture.Application.Tests.TestData;
using Venture.Application.Users;
using Venture.Domain.Social.Follows;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Tests.Social;

public class FollowUserCommandHandlerTests
{
    private readonly IFollowRepository _followRepository = Substitute.For<IFollowRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private readonly FollowUserCommandHandler _handler;

    public FollowUserCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(ApplicationFactory.Now);

        _handler = new FollowUserCommandHandler(
            _followRepository,
            _userRepository,
            _currentUser,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenUserIsNotAuthenticated()
    {
        _currentUser.UserId.Returns((UserId?)null);

        var result = await _handler.Handle(new FollowUserCommand(Guid.NewGuid()), CancellationToken.None);

        result.Error.Should().Be(UserErrors.NotAuthenticated);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenFollowingYourself()
    {
        var userId = UserId.New();
        _currentUser.UserId.Returns(userId);

        var result = await _handler.Handle(new FollowUserCommand(userId.Value), CancellationToken.None);

        result.Error.Should().Be(SocialErrors.CannotFollowYourself);
        _followRepository.DidNotReceive().Add(Arg.Any<Follow>());
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenTargetUserDoesNotExist()
    {
        _currentUser.UserId.Returns(UserId.New());
        _userRepository.ExistsAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new FollowUserCommand(Guid.NewGuid()), CancellationToken.None);

        result.Error.Code.Should().Be("Users.NotFound");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenAlreadyFollowing()
    {
        _currentUser.UserId.Returns(UserId.New());
        _userRepository.ExistsAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(true);
        _followRepository
            .ExistsAsync(Arg.Any<UserId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.Handle(new FollowUserCommand(Guid.NewGuid()), CancellationToken.None);

        result.Error.Should().Be(SocialErrors.AlreadyFollowing);
        _followRepository.DidNotReceive().Add(Arg.Any<Follow>());
    }

    [Fact]
    public async Task Handle_ShouldCreateFollow()
    {
        var followerId = UserId.New();
        var followingId = Guid.NewGuid();

        _currentUser.UserId.Returns(followerId);
        _userRepository.ExistsAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(true);

        Follow? added = null;
        _followRepository.When(repository => repository.Add(Arg.Any<Follow>()))
            .Do(call => added = call.Arg<Follow>());

        var result = await _handler.Handle(new FollowUserCommand(followingId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        added!.FollowerId.Should().Be(followerId);
        added.FollowingId.Value.Should().Be(followingId);
    }
}
