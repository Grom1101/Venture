using FluentValidation;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Users.Commands.UpdateProfile;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(command => command.DisplayName)
            .NotEmpty()
            .MinimumLength(DisplayName.MinLength)
            .MaximumLength(DisplayName.MaxLength);

        RuleFor(command => command.About)
            .NotEmpty()
            .MaximumLength(Bio.MaxAboutLength);

        RuleFor(command => command.FocusedOn)
            .NotEmpty()
            .MaximumLength(Bio.MaxFocusedOnLength);

        RuleFor(command => command.Comment)
            .MaximumLength(Bio.MaxCommentLength);

        RuleFor(command => command.Interests)
            .NotNull()
            .Must(interests => interests.Count <= Bio.MaxInterestsCount)
            .WithMessage($"Нельзя указать более {Bio.MaxInterestsCount} интересов");

        RuleForEach(command => command.Interests)
            .NotEmpty()
            .MaximumLength(Interest.MaxLength);
    }
}
