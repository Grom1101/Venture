using FluentValidation;

namespace Venture.Application.Posts.Commands.LikePost;

public sealed class LikePostCommandValidator : AbstractValidator<LikePostCommand>
{
    public LikePostCommandValidator()
    {
        RuleFor(command => command.PostId)
            .NotEmpty();
    }
}
