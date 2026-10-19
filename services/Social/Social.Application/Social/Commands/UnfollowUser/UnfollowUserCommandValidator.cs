using FluentValidation;

namespace Venture.Application.Social.Commands.UnfollowUser;

public sealed class UnfollowUserCommandValidator : AbstractValidator<UnfollowUserCommand>
{
    public UnfollowUserCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();
    }
}
