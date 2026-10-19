using FluentAssertions;
using FluentValidation;
using Venture.Application.Behaviors;
using Venture.Application.Common.Results;
using Venture.Domain.Common.Results;
using Venture.Application.Users.Commands.RegisterUser;

namespace Venture.Application.Tests.Behaviors;

public class ValidationBehaviorTests
{
    private static readonly RegisterUserCommand ValidCommand =
        new("john@doe.com", "johndoe", "John Doe", "very-secret-password");

    [Fact]
    public async Task Handle_ShouldCallNext_WhenCommandIsValid()
    {
        var behavior = CreateBehavior();
        var handlerCalled = false;

        var result = await behavior.Handle(ValidCommand, _ =>
        {
            handlerCalled = true;

            return Task.FromResult(Result.Success(Guid.NewGuid()));
        }, CancellationToken.None);

        handlerCalled.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenCommandIsInvalid()
    {
        var behavior = CreateBehavior();
        var invalidCommand = new RegisterUserCommand("not-an-email", "ab", "J", "short");
        var handlerCalled = false;

        var result = await behavior.Handle(invalidCommand, _ =>
        {
            handlerCalled = true;

            return Task.FromResult(Result.Success(Guid.NewGuid()));
        }, CancellationToken.None);

        handlerCalled.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Should().BeOfType<ValidationError>()
            .Which.Errors.Should().NotBeEmpty();
    }

    private static ValidationBehavior<RegisterUserCommand, Result<Guid>> CreateBehavior()
    {
        IValidator<RegisterUserCommand>[] validators = [new RegisterUserCommandValidator()];

        return new ValidationBehavior<RegisterUserCommand, Result<Guid>>(validators);
    }
}
