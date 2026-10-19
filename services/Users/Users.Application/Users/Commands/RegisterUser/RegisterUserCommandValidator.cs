using FluentValidation;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Users.Commands.RegisterUser;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(Email.MaxLength);

        RuleFor(command => command.Username)
            .NotEmpty()
            .MinimumLength(Username.MinLength)
            .MaximumLength(Username.MaxLength);

        RuleFor(command => command.DisplayName)
            .NotEmpty()
            .MinimumLength(DisplayName.MinLength)
            .MaximumLength(DisplayName.MaxLength);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MinimumLength(Password.MinLength)
            .MaximumLength(Password.MaxLength);
    }
}
