using FluentValidation;

namespace Venture.Application.Posts.Commands.UnlikePost;

public sealed class UnlikePostCommandValidator : AbstractValidator<UnlikePostCommand>
{
    public UnlikePostCommandValidator()
    {
        RuleFor(command => command.PostId)
            .NotEmpty();
    }
}
