using Venture.Application.Abstractions.Authentication;
using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Persistence;
using Venture.Domain.Common.Results;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Users.Commands.AddSkill;

internal sealed class AddSkillCommandHandler(
    IUserRepository userRepository,
    ICurrentUser currentUser)
    : ICommandHandler<AddSkillCommand>
{
    public async Task<Result> Handle(AddSkillCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } currentUserId)
            return Result.Failure(UserErrors.NotAuthenticated);

        var user = await userRepository.GetByIdAsync(currentUserId, cancellationToken);

        if (user is null)
            return Result.Failure(UserErrors.NotFound(currentUserId.Value));

        var skillName = SkillName.Create(command.Name);
        if (skillName.IsFailure)
            return Result.Failure(skillName.Error);

        var added = user.AddSkill(skillName.Value, command.Level);
        if (added.IsFailure)
            return added;

        return Result.Success();
    }
}
