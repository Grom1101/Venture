using FluentValidation;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Users.Commands.AddSkill;

public sealed class AddSkillCommandValidator : AbstractValidator<AddSkillCommand>
{
    public AddSkillCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(SkillName.MaxLength);

        RuleFor(command => command.Level)
            .IsInEnum();
    }
}
