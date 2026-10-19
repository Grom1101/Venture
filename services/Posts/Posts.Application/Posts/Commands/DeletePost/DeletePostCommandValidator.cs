using FluentValidation;

namespace Venture.Application.Posts.Commands.DeletePost;

public sealed class DeletePostCommandValidator : AbstractValidator<DeletePostCommand>
{
    public DeletePostCommandValidator()
    {
        RuleFor(command => command.PostId)
            .NotEmpty();
    }
}
