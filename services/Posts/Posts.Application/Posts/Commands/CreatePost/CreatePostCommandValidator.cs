using FluentValidation;
using Venture.Domain.Posts.ValueObjects;

namespace Venture.Application.Posts.Commands.CreatePost;

public sealed class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidator()
    {
        RuleFor(command => command.Content)
            .NotEmpty()
            .MaximumLength(PostContent.MaxLength);
    }
}
