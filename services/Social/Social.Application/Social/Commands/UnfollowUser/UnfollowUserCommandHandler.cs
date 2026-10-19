using Venture.Application.Abstractions.Authentication;
using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Persistence;
using Venture.Domain.Common.Results;
using Venture.Application.Users;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Social.Commands.UnfollowUser;

internal sealed class UnfollowUserCommandHandler(
    IFollowRepository followRepository,
    ICurrentUser currentUser)
    : ICommandHandler<UnfollowUserCommand>
{
    public async Task<Result> Handle(UnfollowUserCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } followerId)
            return Result.Failure(UserErrors.NotAuthenticated);

        var followingId = UserId.From(command.UserId);
        if (followingId.IsFailure)
            return Result.Failure(followingId.Error);

        var follow = await followRepository.GetAsync(followerId, followingId.Value, cancellationToken);

        if (follow is null)
            return Result.Failure(SocialErrors.NotFollowing);

        follow.Unfollow();
        followRepository.Remove(follow);

        return Result.Success();
    }
}
