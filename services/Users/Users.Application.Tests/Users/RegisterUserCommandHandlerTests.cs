using FluentAssertions;
using NSubstitute;
using Venture.Application.Abstractions.Authentication;
using Venture.Application.Abstractions.Persistence;
using Venture.Application.Abstractions.Time;
using Venture.Domain.Common.Results;
using Venture.Application.Users;
using Venture.Application.Users.Commands.RegisterUser;
using Venture.Domain.Users;

namespace Venture.Application.Tests.Users;

public class RegisterUserCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static readonly RegisterUserCommand Command =
        new("John@Doe.com", "JohnDoe", "John Doe", "very-secret-password");

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private readonly RegisterUserCommandHandler _handler;

    public RegisterUserCommandHandlerTests()
    {
        _passwordHasher.Hash(Arg.Any<string>()).Returns("salt.hash");
        _dateTimeProvider.UtcNow.Returns(Now);

        _handler = new RegisterUserCommandHandler(_userRepository, _passwordHasher, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_ShouldAddUser_WhenUsernameAndEmailAreFree()
    {
        var result = await _handler.Handle(Command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _userRepository.Received(1).Add(Arg.Is<User>(user =>
            user.Username.Value == "johndoe" &&
            user.Email.Value == "john@doe.com" &&
            user.CreatedAtUtc == Now));
    }

    [Fact]
    public async Task Handle_ShouldReturnCreatedUserId()
    {
        User? added = null;
        _userRepository.When(repository => repository.Add(Arg.Any<User>()))
            .Do(call => added = call.Arg<User>());

        var result = await _handler.Handle(Command, CancellationToken.None);

        result.Value.Should().Be(added!.Id.Value);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenUsernameIsTaken()
    {
        _userRepository
            .IsUsernameTakenAsync(Arg.Any<Domain.Users.ValueObjects.Username>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.Handle(Command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.UsernameAlreadyTaken);
        result.Error.Type.Should().Be(ErrorType.Conflict);
        _userRepository.DidNotReceive().Add(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenEmailIsTaken()
    {
        _userRepository
            .IsEmailTakenAsync(Arg.Any<Domain.Users.ValueObjects.Email>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.Handle(Command, CancellationToken.None);

        result.Error.Should().Be(UserErrors.EmailAlreadyTaken);
        _userRepository.DidNotReceive().Add(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_ShouldStoreHashedPassword()
    {
        User? added = null;
        _userRepository.When(repository => repository.Add(Arg.Any<User>()))
            .Do(call => added = call.Arg<User>());

        await _handler.Handle(Command, CancellationToken.None);

        added!.PasswordHash.Value.Should().Be("salt.hash");
        _passwordHasher.Received(1).Hash("very-secret-password");
    }
}
