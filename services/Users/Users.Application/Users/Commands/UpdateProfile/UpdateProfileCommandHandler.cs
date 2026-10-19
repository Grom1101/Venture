using Venture.Application.Abstractions.Authentication;
using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Persistence;
using Venture.Domain.Common.Results;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Users.Commands.UpdateProfile;

internal sealed class UpdateProfileCommandHandler(
    IUserRepository userRepository,
    ICurrentUser currentUser)
    : ICommandHandler<UpdateProfileCommand>
{
    public async Task<Result> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } currentUserId)
            return Result.Failure(UserErrors.NotAuthenticated);

        var user = await userRepository.GetByIdAsync(currentUserId, cancellationToken);

        if (user is null)
            return Result.Failure(UserErrors.NotFound(currentUserId.Value));

        var interests = new List<Interest>(command.Interests.Count);
        foreach (var value in command.Interests)
        {
            var interest = Interest.Create(value);
            if (interest.IsFailure)
                return Result.Failure(interest.Error);

            interests.Add(interest.Value);
        }

        var displayName = DisplayName.Create(command.DisplayName);
        if (displayName.IsFailure)
            return Result.Failure(displayName.Error);

        var bio = Bio.Create(
            command.About,
            command.FocusedOn,
            command.Comment ?? string.Empty,
            interests);

        if (bio.IsFailure)
            return Result.Failure(bio.Error);

        var changed = user.ChangeProfile(displayName.Value, bio.Value);
        if (changed.IsFailure)
            return changed;

        return Result.Success();
    }
}
