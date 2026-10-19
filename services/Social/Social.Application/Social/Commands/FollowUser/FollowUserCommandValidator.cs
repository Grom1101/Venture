using FluentValidation;

namespace Venture.Application.Social.Commands.FollowUser;

public sealed class FollowUserCommandValidator : AbstractValidator<FollowUserCommand>
{
    public FollowUserCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();
    }
}
