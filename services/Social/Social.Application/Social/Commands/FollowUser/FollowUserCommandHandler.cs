using Venture.Application.Abstractions.Authentication;
using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Persistence;
using Venture.Application.Abstractions.Time;
using Venture.Domain.Common.Results;
using Venture.Application.Users;
using Venture.Domain.Social.Follows;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Social.Commands.FollowUser;

/// <summary>
/// Пример разделения ответственности: существование цели проверяет application,
/// запрет подписки на себя — домен, уникальность пары — unique-индекс в БД.
/// </summary>
internal sealed class FollowUserCommandHandler(
    IFollowRepository followRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<FollowUserCommand>
{
    public async Task<Result> Handle(FollowUserCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } followerId)
            return Result.Failure(UserErrors.NotAuthenticated);

        var followingId = UserId.From(command.UserId);
        if (followingId.IsFailure)
            return Result.Failure(followingId.Error);

        if (followerId == followingId.Value)
            return Result.Failure(SocialErrors.CannotFollowYourself);

        if (!await userRepository.ExistsAsync(followingId.Value, cancellationToken))
            return Result.Failure(UserErrors.NotFound(command.UserId));

        if (await followRepository.ExistsAsync(followerId, followingId.Value, cancellationToken))
            return Result.Failure(SocialErrors.AlreadyFollowing);

        var follow = Follow.Create(followerId, followingId.Value, dateTimeProvider.UtcNow);
        if (follow.IsFailure)
            return Result.Failure(follow.Error);

        followRepository.Add(follow.Value);

        return Result.Success();
    }
}
