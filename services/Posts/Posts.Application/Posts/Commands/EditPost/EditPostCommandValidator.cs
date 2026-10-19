using FluentValidation;
using Venture.Domain.Posts.ValueObjects;

namespace Venture.Application.Posts.Commands.EditPost;

public sealed class EditPostCommandValidator : AbstractValidator<EditPostCommand>
{
    public EditPostCommandValidator()
    {
        RuleFor(command => command.PostId)
            .NotEmpty();

        RuleFor(command => command.Content)
            .NotEmpty()
            .MaximumLength(PostContent.MaxLength);
    }
}
